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
using Discord;
using Discord.WebSocket;
using Pahoe.Payloads;
using Pahoe.Search;

namespace Pahoe
{
    public sealed class LavalinkClient
    {
        public bool IsReady { get; private set; }
        public Func<LavalinkPlayer, LavalinkTrack, TrackEndReason, Task> OnTrackEnded { get; set; }

        internal BaseSocketClient Discord { get; }
        internal ClientWebSocket WebSocket { get; } = new ClientWebSocket();
        internal ConcurrentDictionary<ulong, LavalinkPlayer> Players { get; } = new ConcurrentDictionary<ulong, LavalinkPlayer>();

        private LavalinkConfiguration Configuration { get; }
        private HttpClient HttpClient { get; } = new HttpClient();
        private CancellationTokenSource CancellationSource { get; } = new CancellationTokenSource();
        private CancellationToken Token { get; }
        private string SearchEndpoint { get; }

        private LavalinkClient(BaseSocketClient discordClient, LavalinkConfiguration configuration)
        {
            Discord = discordClient;
            Configuration = configuration;

            Token = CancellationSource.Token;
            SearchEndpoint = string.Format("http://{0}:{1}/loadtracks?identifier=", Configuration.Address, Configuration.Port);
        }

        public LavalinkClient(DiscordSocketClient discordClient, LavalinkConfiguration configuration) : this(discordClient as BaseSocketClient, configuration) { }
        public LavalinkClient(DiscordShardedClient discordShardedClient, LavalinkConfiguration configuration) : this(discordShardedClient as BaseSocketClient, configuration) { }

        public async Task StartAsync()
        {
            if (Discord.CurrentUser == null)
                throw new InvalidOperationException("Discord client must be logged in and ready.");

            ClientWebSocketOptions options = WebSocket.Options;
            options.SetRequestHeader("Authorization", Configuration.Authorization);
            options.SetRequestHeader("User-Id", Discord.CurrentUser.Id.ToString());
            options.SetRequestHeader("Num-Shards", Configuration.Shards.ToString());

            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Configuration.Authorization);

            Discord.VoiceServerUpdated += VoiceServerUpdatedAsync;

            await WebSocket.ConnectAsync(new Uri(string.Format("ws://{0}:{1}/", Configuration.Address, Configuration.Port)), Token).ConfigureAwait(false);
            _ = Task.Run(WebSocketReceiveAsync, Token);

            IsReady = true;
        }

        public async Task StopAsync()
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing...", default).ConfigureAwait(false);
            CancellationSource.Cancel();
            Discord.VoiceServerUpdated -= VoiceServerUpdatedAsync;

            foreach (LavalinkPlayer player in Players.Values)
                await player.DisconnectAsync().ConfigureAwait(false);

            WebSocket.Dispose();
            HttpClient.Dispose();
        }

        public async ValueTask<LavalinkPlayer> ConnectAsync(IVoiceChannel voiceChannel)
        {
            if (Players.TryGetValue(voiceChannel.GuildId, out LavalinkPlayer player))
                return player;


            player = new LavalinkPlayer(this, voiceChannel);
            Players.TryAdd(voiceChannel.GuildId, player);

            await voiceChannel.ConnectAsync(selfDeaf: Configuration.SelfDeaf, external: true).ConfigureAwait(false);
            return player;
        }

        public Task DisconnectAsync(LavalinkPlayer player)
            => player.DisconnectAsync();

        public Task<SearchResult> SearchYouTubeAsync(string search)
            => SearchAsync(string.Concat("ytsearch:", search));

        public async Task<SearchResult> SearchAsync(string search)
        {
            // Have to write a url encoder that uses Spans...
            using Stream stream = await HttpClient.GetStreamAsync(string.Concat(SearchEndpoint, WebUtility.UrlEncode(search))).ConfigureAwait(false);
            return SearchResult.FromStream(stream);
        }

        private Task VoiceServerUpdatedAsync(SocketVoiceServer voiceServer) => VoiceServerUpdated.SendAsync(this, voiceServer).AsTask();

        private async Task WebSocketReceiveAsync()
        {
            Memory<byte> buffer = new byte[4096];

            while (!Token.IsCancellationRequested)
            {
                ValueWebSocketReceiveResult result;
                int bytesRead = 0;

                do
                {
                    result = await WebSocket.ReceiveAsync(buffer.Slice(bytesRead), Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Do something...
                    }

                    bytesRead += result.Count;
                    if (bytesRead == buffer.Length)
                    {
                        // Skip over unusually large message?
                        bytesRead = 0;
                        while (!(await WebSocket.ReceiveAsync(buffer, Token).ConfigureAwait(false)).EndOfMessage) ;
                    }
                }
                while (!result.EndOfMessage);

                Memory<byte> data = buffer.Slice(0, bytesRead);

                // Parse received json...
                await HandleIncomingMessageAsync(data.Span).ConfigureAwait(false);
            }
        }

        private async Task TrackEndedAsync(LavalinkPlayer player, LavalinkTrack track, TrackEndReason reason)
        {
            if (OnTrackEnded != null)
            {
                Delegate[] delegates = OnTrackEnded.GetInvocationList();
                for (int i = 0; i < delegates.Length; i++)
                {
                    try
                    {
                        await ((Func<LavalinkPlayer, LavalinkTrack, TrackEndReason, Task>) delegates[i])(player, track, reason).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Log...?
                    }
                }
            }
        }

        private Task HandleIncomingMessageAsync(Span<byte> data)
        {
            Utf8JsonReader opCodeReader = new Utf8JsonReader(data);
            Utf8JsonReader reader = new Utf8JsonReader(data);
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
                if (Players.TryGetValue(guildId, out LavalinkPlayer player))
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
                        ReadOnlySpan<byte> bytes = reader.ValueSpan;
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
                    // TODO
                }
                else if (Players.TryGetValue(guildId, out LavalinkPlayer player))
                {
                    var track = LavalinkTrack.Decode(Encoding.UTF8.GetString(trackHashSpan));
                    if (equals(type, "TrackEndEvent"))
                        return TrackEndedAsync(player, track, endReason);
                }
            }

            return Task.CompletedTask;
        }
    }
}
