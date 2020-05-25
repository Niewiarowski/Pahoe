using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Pahoe.Payloads;
using Pahoe.Search;

namespace Pahoe
{
    public sealed class LavalinkPlayer
    {
        public CachedVoiceChannel Channel { get; private set; }

        public LavalinkTrack Track { get; private set; }

        public TimeSpan Position { get; internal set; }

        public PlayerState State { get; private set; } = PlayerState.Idle;

        public ushort Volume { get; private set; } = 100;

        public BandCollection Bands { get; }

        internal readonly LavalinkClient Client;
        internal readonly ulong GuildId;
        internal readonly string GuildIdStr;
        internal string SessionId;

        internal LavalinkPlayer(LavalinkClient client, CachedVoiceChannel channel)
        {
            Bands = new BandCollection(this);

            Client = client;
            Channel = channel;
            GuildId = channel.Guild.Id;
            GuildIdStr = channel.Guild.Id.ToString();

            Client.DiscordClient.VoiceStateUpdated += VoiceStateUpdatedAsync;
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
            Client.DiscordClient.VoiceStateUpdated -= VoiceStateUpdatedAsync;
            Client.Players.TryRemove(GuildId, out _);
            State = PlayerState.Idle;
            await Destroy.SendAsync(this).ConfigureAwait(false);

            if (Channel != null)
                await Channel.Client.UpdateVoiceStateAsync(Channel.Guild.Id, null).ConfigureAwait(false);
        }

        private Task VoiceStateUpdatedAsync(VoiceStateUpdatedEventArgs e)
        {
            if (Client.DiscordClient.CurrentUser.Id == e.Member.Id)
            {
                if (e.NewVoiceState == null)
                    return DisconnectAsync();

                Channel = Channel.Guild.GetVoiceChannel(e.NewVoiceState.ChannelId);
                SessionId = e.NewVoiceState.SessionId;
            }

            return Task.CompletedTask;
        }
    }
}
