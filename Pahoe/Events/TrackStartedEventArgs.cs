using Pahoe.Search;

namespace Pahoe.Events
{
    public class TrackStartedEventArgs : TrackEventArgs
    {
        internal TrackStartedEventArgs(LavalinkTrack track) : base(track) { }
    }
}
