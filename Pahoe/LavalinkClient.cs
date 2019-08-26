using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Pahoe.Search;
using Pahoe.Payloads.Outgoing;
using Discord;
using Discord.WebSocket;

namespace Pahoe
{
    public sealed class LavalinkClient
    {
        public bool IsReady { get; private set; }

        internal BaseSocketClient Discord { get; }
        internal ClientWebSocket WebSocket { get; } = new ClientWebSocket();
        internal ConcurrentDictionary<ulong, LavalinkPlayer> Players { get; } = new ConcurrentDictionary<ulong, LavalinkPlayer>();

        private PahoeConfiguration Configuration { get; }
        private HttpClient HttpClient { get; } = new HttpClient();
        private CancellationTokenSource CancellationSource { get; } = new CancellationTokenSource();
        private CancellationToken Token { get; }
        private string SearchEndpoint { get; set; }

        private LavalinkClient(BaseSocketClient discordClient, PahoeConfiguration configuration)
        {
            Discord = discordClient;
            Configuration = configuration;

            Token = CancellationSource.Token;
        }

        public LavalinkClient(DiscordSocketClient discordClient, PahoeConfiguration configuration) : this(discordClient as BaseSocketClient, configuration) { }
        public LavalinkClient(DiscordShardedClient discordShardedClient, PahoeConfiguration configuration) : this(discordShardedClient as BaseSocketClient, configuration) { }

        public async Task StartAsync()
        {
            if (Discord.CurrentUser == null)
                throw new InvalidOperationException("Discord client must be logged in and ready.");

            ClientWebSocketOptions options = WebSocket.Options;
            options.SetRequestHeader("Authorization", Configuration.Password);
            options.SetRequestHeader("User-Id", Discord.CurrentUser.Id.ToString());
            options.SetRequestHeader("Num-Shards", Configuration.Shards.ToString());

            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Configuration.Password);
            SearchEndpoint = string.Format("http://{0}:{1}/loadtracks?identifier=", Configuration.Address, Configuration.Port);

            Discord.VoiceServerUpdated += VoiceServerUpdatedAsync;

            await WebSocket.ConnectAsync(new Uri(string.Format("ws://{0}:{1}/", Configuration.Address, Configuration.Port)), Token).ConfigureAwait(false);
            _ = Task.Run(WebSocketReceiveAsync, Token);

            IsReady = true;
        }

        public async ValueTask<LavalinkPlayer> ConnectAsync(IVoiceChannel voiceChannel)
        {
            if (Players.TryGetValue(voiceChannel.GuildId, out LavalinkPlayer player))
                return player;

            _ = await voiceChannel.ConnectAsync(selfDeaf: Configuration.SelfDeaf, external: true);

            player = new LavalinkPlayer(this, voiceChannel);
            Players.TryAdd(voiceChannel.GuildId, player);
            return player;
        }

        public async Task<SearchResult> SearchAsync(string search)
        {
            // Have to write a url encoder that uses Spans...
            using Stream stream = await HttpClient.GetStreamAsync(string.Concat(SearchEndpoint, WebUtility.UrlEncode(search)));
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
                        while (!(await WebSocket.ReceiveAsync(buffer, Token)).EndOfMessage) ;
                    }
                }
                while (!result.EndOfMessage);

                Memory<byte> data = buffer.Slice(0, bytesRead);
                Console.WriteLine(Encoding.UTF8.GetString(data.Span));

                // Parse received json...
            }
        }
    }
}
