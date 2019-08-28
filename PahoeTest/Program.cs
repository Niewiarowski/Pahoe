using Discord;
using Discord.WebSocket;
using Pahoe;
using Pahoe.Search;
using System;
using System.Threading.Tasks;

namespace PahoeTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            LavalinkTrack track = LavalinkTrack.Decode("QAAAjwIAK1Rlc3RpbmcgaWYgU2hhcmtzIENhbiBTbWVsbCBhIERyb3Agb2YgQmxvb2QACk1hcmsgUm9iZXIAAAAAAA5EWAALdWdSYzVqeDgweWcAAQAraHR0cHM6Ly93d3cueW91dHViZS5jb20vd2F0Y2g/dj11Z1JjNWp4ODB5ZwAHeW91dHViZQAAAAAAAAAA");
            DiscordSocketClient discord = new DiscordSocketClient();
            await discord.LoginAsync(TokenType.Bot, "token");

            LavalinkClient pahoe = new LavalinkClient(discord, new LavalinkConfiguration
            {
                Authorization = "password"
            });

            await discord.StartAsync();

            discord.Ready += async () =>
            {
                await pahoe.StartAsync();
                SearchResult result = await pahoe.SearchYouTubeAsync("kfchvCyHmsc");

                IVoiceChannel vc = discord.GetChannel(416711632702537738) as IVoiceChannel;
                LavalinkPlayer player = await pahoe.ConnectAsync(vc);
                await player.PlayAsync(result.Tracks[0]);

            };

            await Task.Delay(-1);
        }
    }
}
