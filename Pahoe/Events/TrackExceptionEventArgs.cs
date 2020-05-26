using Pahoe.Search;

namespace Pahoe.Events
{
    public class TrackExceptionEventArgs : TrackEventArgs
    {
        public string Error { get; }

        public TrackExceptionEventArgs(LavalinkTrack track, string error) : base(track)
        {
            Error = error;
        }
    }
}
