using System;
using Pahoe.Search;

namespace Pahoe.Events
{
    public class TrackEventArgs : EventArgs
    {
        public LavalinkTrack Track { get; }

        internal TrackEventArgs(LavalinkTrack track)
        {
            Track = track;
        }
    }
}
