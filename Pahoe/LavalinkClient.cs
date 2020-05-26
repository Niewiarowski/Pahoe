using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Pahoe.Payloads;
using Pahoe.Search;

namespace Pahoe
{
    // TODO: IDisposable (_cts, _http, WebSocket)
    // TODO: make the client stoppable and then resumable (_Cts)
    public sealed class LavalinkClient
    {
        public string Address { get; }

        public int Port { get; }

        public string Authorization { get; }

        public int Shards { get; }

        public bool SelfDeaf { get; }

        public bool IsReady { get; private set; }

        internal ClientWebSocket WebSocket { get; private set; }

        internal readonly DiscordClientBase DiscordClient;
        internal readonly ConcurrentDictionary<ulong, LavalinkPlayer> Players = new ConcurrentDictionary<ulong, LavalinkPlayer>();

        private readonly string _connectionEndpoint;
        private readonly string _searchEndpoint;
        private readonly HttpClient _http = new HttpClient();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public LavalinkClient(DiscordClientBase client, LavalinkConfiguration configuration)
        {
            DiscordClient = client;

            Address = configuration.Address;
            Port = configuration.Port;
            Authorization = configuration.Authorization;
            Shards = configuration.Shards;
            SelfDeaf = configuration.SelfDeaf;

            _connectionEndpoint = string.Format("{0}:{1}", configuration.Address, configuration.Port);
            _searchEndpoint = string.Format("http://{0}/loadtracks?identifier=", _connectionEndpoint);
        }

        public async Task StartAsync()
        {
            if (DiscordClient.CurrentUser == null)
                throw new InvalidOperationException("Discord client must be logged in and ready.");

            _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Authorization);

            DiscordClient.VoiceServerUpdated += VoiceServerUpdatedAsync;

            await ConnectWebSocketAsync();
            _ = Task.Run(WebSocketReceiveAsync, _cts.Token);

            IsReady = true;
        }

        public async Task StopAsync()
        {
            DiscordClient.VoiceServerUpdated -= VoiceServerUpdatedAsync;

            foreach (var player in Players.Values)
                await player.DisconnectAsync().ConfigureAwait(false);

            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing...", default).ConfigureAwait(false);
            _cts.Cancel();
            _cts.Dispose();
            WebSocket.Dispose();
            _http.Dispose();
        }

        public async ValueTask<LavalinkPlayer> ConnectAsync(CachedVoiceChannel channel)
        {
            if (Players.TryGetValue(channel.Guild.Id, out var player))
            {
                if (channel.Id != player.Channel.Id)
                    await DiscordClient.UpdateVoiceStateAsync(channel.Guild.Id, channel.Id, isDeafened: SelfDeaf).ConfigureAwait(false);

                return player;
            }

            player = new LavalinkPlayer(this, channel);
            Players.TryAdd(channel.Guild.Id, player);

            await DiscordClient.UpdateVoiceStateAsync(channel.Guild.Id, channel.Id, isDeafened: SelfDeaf).ConfigureAwait(false);
            return player;
        }

        public Task DisconnectAsync(LavalinkPlayer player)
            => player.DisconnectAsync();

        public bool TryGetPlayer(ulong guildId, out LavalinkPlayer player)
            => Players.TryGetValue(guildId, out player);

        public Task<SearchResult> SearchYouTubeAsync(string search)
            => SearchAsync(string.Concat("ytsearch:", search));

        public async Task<SearchResult> SearchAsync(string search)
        {
            // Have to write a url encoder that uses Spans...
            using Stream stream = await _http.GetStreamAsync(string.Concat(_searchEndpoint, WebUtility.UrlEncode(search))).ConfigureAwait(false);
            return SearchResult.FromStream(stream);
        }

        private Task VoiceServerUpdatedAsync(VoiceServerUpdatedEventArgs e)
        {
            // Shouldn't ever be false.
            if (!Players.TryGetValue(e.Guild.Id, out var player))
                return Task.CompletedTask;

            return VoiceServerUpdated.SendAsync(player, e).AsTask();
        }

        private async Task ConnectWebSocketAsync()
        {
            WebSocket = new ClientWebSocket();

            var options = WebSocket.Options;
            options.SetRequestHeader("Authorization", Authorization);
            options.SetRequestHeader("User-Id", DiscordClient.CurrentUser.Id.ToString());
            options.SetRequestHeader("Num-Shards", Shards.ToString());
            options.SetRequestHeader("Resume-Key", ConfigureResume.ResumeKey);

            await WebSocket.ConnectAsync(new Uri(string.Format("ws://{0}/", _connectionEndpoint)), _cts.Token).ConfigureAwait(false);
            await ConfigureResume.SendAsync(WebSocket);
        }

        private async Task WebSocketReceiveAsync()
        {
            Memory<byte> buffer = new byte[4096];

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    ValueWebSocketReceiveResult result;
                    int bytesRead = 0;

                    do
                    {
                        result = await WebSocket.ReceiveAsync(buffer.Slice(bytesRead), _cts.Token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Do something...
                            await ConnectWebSocketAsync();
                            continue;
                        }

                        bytesRead += result.Count;
                        if (bytesRead == buffer.Length)
                        {
                            // Skip over unusually large message?
                            bytesRead = 0;
                            while (!(await WebSocket.ReceiveAsync(buffer, _cts.Token).ConfigureAwait(false)).EndOfMessage)
                                ;
                        }
                    }
                    while (!result.EndOfMessage && !_cts.IsCancellationRequested);

                    if (result.MessageType == WebSocketMessageType.Close)
                        continue;

                    // Parse received json...
                    var data = buffer.Slice(0, bytesRead);
                    await HandleIncomingMessageAsync(data.Span).ConfigureAwait(false);
                }
                catch (WebSocketException)
                {
                    // Disconnected...

                    while (!_cts.IsCancellationRequested)
                    {
                        try
                        {
                            // In desperate need of some logging
                            await Task.Delay(5000);
                            await ConnectWebSocketAsync();
                            break;
                        }
                        catch { }
                    }
                }
                catch (Exception e)
                {
                    // Jesus christ we should log this!!!
                }
            }

        }

        private Task HandleIncomingMessageAsync(Span<byte> data)
        {
            var opCodeReader = new Utf8JsonReader(data);
            var reader = new Utf8JsonReader(data);
            ReadOnlySpan<byte> op = default;

            while (opCodeReader.Read())
            {
                if (opCodeReader.TokenType == JsonTokenType.PropertyName)
                {
                    if (opCodeReader.ValueTextEquals("op"))
                    {
                        opCodeReader.Skip();
                        op = opCodeReader.ValueSpan;
                        break;
                    }
                }
            }

            static bool equals(ReadOnlySpan<byte> bytes, ReadOnlySpan<char> str)
            {
                for (int i = 0; i < bytes.Length; i++)
                    if (bytes[i] != (byte) str[i])
                        return false;

                return true;
            }

            if (equals(op, "playerUpdate"))
            {
                double position = 0;
                ulong guildId = 0;

                while (reader.Read() && (position == 0 || guildId == 0))
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        if (reader.ValueTextEquals("guildId"))
                        {
                            reader.Skip();
                            Span<char> guildIdChars = stackalloc char[reader.ValueSpan.Length];
                            Encoding.ASCII.GetChars(reader.ValueSpan, guildIdChars);
                            ulong.TryParse(guildIdChars, out guildId);
                        }
                        else if (reader.ValueTextEquals("state"))
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    if (reader.ValueTextEquals("position"))
                                    {
                                        reader.Skip();
                                        position = reader.GetDouble();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Should always be true
                if (Players.TryGetValue(guildId, out var player))
                    player.Position = TimeSpan.FromMilliseconds(position);
            }
            else if (equals(op, "event"))
            {
                ReadOnlySpan<byte> type = default;
                while (opCodeReader.Read())
                {
                    if (opCodeReader.TokenType == JsonTokenType.PropertyName)
                    {
                        if (opCodeReader.ValueTextEquals("type"))
                        {
                            opCodeReader.Skip();
                            type = opCodeReader.ValueSpan;
                            break;
                        }
                    }
                }

                ReadOnlySpan<byte> trackHashSpan = default;
                ReadOnlySpan<byte> errorSpan = default;
                TrackEndReason endReason = default;
                ulong thresholdMs = 0;
                ulong guildId = 0;
                int code = 0;
                bool byRemote = false;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var bytes = reader.ValueSpan;
                        reader.Skip();

                        if (equals(bytes, "track"))
                            trackHashSpan = reader.ValueSpan;
                        else if (equals(bytes, "reason"))
                            endReason = (TrackEndReason) reader.ValueSpan[0];
                        else if (equals(bytes, "error"))
                            errorSpan = reader.ValueSpan;
                        else if (equals(bytes, "thresholdMs"))
                            thresholdMs = reader.GetUInt64();
                        else if (equals(bytes, "code"))
                            code = reader.GetInt32();
                        else if (equals(bytes, "byRemote"))
                            byRemote = reader.GetBoolean();
                        else if (equals(bytes, "guildId"))
                        {
                            Span<char> guildIdChars = stackalloc char[reader.ValueSpan.Length];
                            Encoding.ASCII.GetChars(reader.ValueSpan, guildIdChars);
                            ulong.TryParse(guildIdChars, out guildId);
                        }
                    }
                }

                if (equals(type, "WebSocketClosedEvent"))
                {
                    //What the fuck?
                }
                else if (Players.TryGetValue(guildId, out LavalinkPlayer player))
                {
                    var track = LavalinkTrack.Decode(Encoding.UTF8.GetString(trackHashSpan));
                    if (equals(type, "TrackStartEvent"))
                        return player.TrackStartedAsync(track);
                    else if (equals(type, "TrackEndEvent"))
                        return player.TrackEndedAsync(track, endReason);
                    else if (equals(type, "TrackExceptionEvent"))
                        return player.TrackExceptionAsync(track, Encoding.UTF8.GetString(errorSpan));
                    else if (equals(type, "TrackStuckEvent"))
                        return player.TrackStuckAsync(track, TimeSpan.FromMilliseconds((double)thresholdMs));
                }
            }

            return Task.CompletedTask;
        }
    }
}
