using System;
using System.Threading.Tasks;
using Disqord;
using Pahoe;
using Pahoe.Search;

namespace PahoeTest
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            using (var client = new DiscordClient(TokenType.Bot, Environment.GetEnvironmentVariable("NOT_QUAHU")))
            {
                var pahoe = new LavalinkClient(client, new LavalinkConfiguration());
                client.Ready += async e =>
                {
                    await pahoe.StartAsync();
                    var result = await pahoe.SearchYouTubeAsync("https://www.youtube.com/watch?v=XZDt-O6r7rM");
                    var channel = client.GetChannel(412378943354568715) as CachedVoiceChannel;
                    var player = await pahoe.ConnectAsync(channel);
                    await player.SetVolumeAsync(10);
                    await player.PlayAsync(result.Tracks[0]);
                };

                await client.RunAsync();
            }
        }
    }
}
