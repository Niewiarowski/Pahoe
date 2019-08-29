using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Pahoe.Payloads;
using Pahoe.Search;

namespace Pahoe
{
    public class LavalinkPlayer
    {
        public IVoiceChannel VoiceChannel { get; }

        public LavalinkTrack Track { get; private set; }

        public TimeSpan Position { get; internal set; }

        public PlayerState State { get; private set; } = PlayerState.Idle;

        public ushort Volume { get; private set; } = 100;

        public Memory<float> Bands { get; } = new float[15];

        internal LavalinkClient Client { get; }
        internal string GuildIdStr { get; }
        internal string SessionId { get; private set; }

        internal LavalinkPlayer(LavalinkClient client, IVoiceChannel voiceChannel)
        {
            Client = client;
            VoiceChannel = voiceChannel;
            GuildIdStr = voiceChannel.GuildId.ToString();

            Client.Discord.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        public ValueTask PlayAsync(LavalinkTrack track, TimeSpan startTime = default, TimeSpan endTime = default, bool noReplace = false)
        {
            Track = track;
            State = PlayerState.Playing;
            return Play.SendAsync(this, track, startTime, endTime, noReplace);
        }

        public ValueTask StopAsync()
        {
            State = PlayerState.Idle;
            return Stop.SendAsync(this);
        }

        public ValueTask PauseAsync()
        {
            State = PlayerState.Paused;
            return Pause.SendAsync(this, true);
        }

        public ValueTask ResumeAsync()
        {
            State = PlayerState.Playing;
            return Pause.SendAsync(this, false);
        }

        public ValueTask SeekAsync(TimeSpan position)
            => Seek.SendAsync(this, (uint) position.TotalMilliseconds);

        public ValueTask SetVolumeAsync(ushort volume)
        {
            Volume = volume;
            return Payloads.Volume.SendAsync(this, volume);
        }

        public ValueTask UpdateBandsAsync()
            => Equalizer.SendAsync(this);

        public async Task DisconnectAsync()
        {
            Client.Discord.UserVoiceStateUpdated -= UserVoiceStateUpdated;
            Client.Players.TryRemove(VoiceChannel.GuildId, out _);
            State = PlayerState.Idle;
            await Destroy.SendAsync(this).ConfigureAwait(false);
            await VoiceChannel.DisconnectAsync().ConfigureAwait(false);
        }

        internal Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState state)
        {
            if (Client.Discord.CurrentUser.Id == user.Id && state.VoiceChannel?.Id == VoiceChannel.Id)
                SessionId = state.VoiceSessionId;

            return Task.CompletedTask;
        }
    }
}
