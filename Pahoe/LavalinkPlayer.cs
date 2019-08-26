using Discord;
using Discord.WebSocket;
using Pahoe.Payloads.Outgoing;
using Pahoe.Search;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pahoe
{
    public class LavalinkPlayer
    {
        public IVoiceChannel VoiceChannel { get; }

        internal LavalinkClient Client { get; }
        internal string GuildIdStr { get; set; }
        internal string SessionId { get; private set; }

        internal LavalinkPlayer(LavalinkClient client, IVoiceChannel voiceChannel)
        {
            Client = client;
            VoiceChannel = voiceChannel;
            GuildIdStr = voiceChannel.GuildId.ToString();

            Client.Discord.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        public ValueTask PlayAsync(LavalinkTrack track, TimeSpan startTime = default, TimeSpan endTime = default, bool noReplace = false)
            => Play.SendAsync(this, track, startTime, endTime, noReplace);

        internal Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState state)
        {
            if (Client.Discord.CurrentUser.Id == user.Id && state.VoiceChannel?.Id == VoiceChannel.Id)
                SessionId = state.VoiceSessionId;

            return Task.CompletedTask;
        }

        internal void Dispose()
        {
            Client.Discord.UserVoiceStateUpdated -= UserVoiceStateUpdated;
        }
    }
}
