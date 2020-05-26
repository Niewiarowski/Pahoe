using Pahoe.Search;

namespace Pahoe.Events
{
    public class TrackEndedEventArgs : TrackEventArgs
    {
        public TrackEndReason Reason { get; }

        internal TrackEndedEventArgs(LavalinkTrack track, TrackEndReason reason) : base(track)
        {
            Reason = reason;
        }
    }
}
