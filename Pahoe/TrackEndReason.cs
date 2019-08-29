namespace Pahoe
{
    public enum TrackEndReason : byte
    {
        Cleanup = (byte) 'C',

        Finished = (byte) 'F',

        LoadFailed = (byte) 'L',

        Replaced = (byte) 'R',

        Stopped = (byte) 'S'
    }
}
