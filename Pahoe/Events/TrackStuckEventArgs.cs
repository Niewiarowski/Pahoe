using System;
using Pahoe.Search;

namespace Pahoe.Events
{
    public class TrackStuckEventArgs : TrackEventArgs
    {
        public TimeSpan Position { get; }

        public TrackStuckEventArgs(LavalinkTrack track, TimeSpan position) : base(track)
        {
            Position = position;
        }
    }
}
