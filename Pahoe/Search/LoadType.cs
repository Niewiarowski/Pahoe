namespace Pahoe.Search
{
    public enum LoadType : byte
    {
        // TODO: remove this considering SearchException is thrown on failures?
        LoadFailed = (byte) 'L',

        NoMatches = (byte) 'N',

        PlaylistLoaded = (byte) 'P',

        SearchResult = (byte) 'S',

        TrackLoaded = (byte) 'T'
    }
}
