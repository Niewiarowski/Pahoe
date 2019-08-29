namespace Pahoe.Search
{
    public enum LoadType : byte
    {
        TrackLoaded = (byte) 'T',

        PlaylistLoaded = (byte) 'P',

        SearchResult = (byte) 'S',

        NoMatches = (byte) 'N',

        // TODO: remove this considering SearchException is thrown on failures?
        LoadFailed = (byte) 'L'
    }
}
