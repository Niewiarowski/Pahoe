﻿using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Pahoe.Payloads;
using Pahoe.Search;

namespace Pahoe
{
    public sealed class LavalinkPlayer
    {
        public SocketVoiceChannel VoiceChannel { get; private set; }

        public LavalinkTrack Track { get; private set; }

        public TimeSpan Position { get; internal set; }

        public PlayerState State { get; private set; } = PlayerState.Idle;

        public ushort Volume { get; private set; } = 100;

        public BandCollection Bands { get; }

        internal readonly LavalinkClient Client;
        internal readonly ulong GuildId;
        internal readonly string GuildIdStr;
        internal string SessionId;

        internal LavalinkPlayer(LavalinkClient client, SocketVoiceChannel voiceChannel)
        {
            Bands = new BandCollection(this);

            Client = client;
            VoiceChannel = voiceChannel;
            GuildId = voiceChannel.Guild.Id;
            GuildIdStr = voiceChannel.Guild.Id.ToString();

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
            Client.Players.TryRemove(GuildId, out _);
            State = PlayerState.Idle;
            await Destroy.SendAsync(this).ConfigureAwait(false);

            if (VoiceChannel != null)
                await VoiceChannel.DisconnectAsync().ConfigureAwait(false);
        }

        private Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState state)
        {
            if (Client.Discord.CurrentUser.Id == user.Id)
            {
                VoiceChannel = state.VoiceChannel;
                SessionId = state.VoiceSessionId;

                if (VoiceChannel == null)
                    return DisconnectAsync();
            }

            return Task.CompletedTask;
        }
    }
}
