namespace Pahoe.Search
{
    public sealed class PlaylistInfo
    {
        public string Name { get; }

        public int SelectedTrack { get; }

        internal PlaylistInfo(string name, int selectedTrack)
        {
            Name = name;
            SelectedTrack = selectedTrack;
        }
    }
}
