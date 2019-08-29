namespace Pahoe
{
    public sealed class LavalinkConfiguration
    {
        public string Address { get; set; } = "127.0.0.1";

        public uint Port { get; set; } = 2333;

        public string Authorization { get; set; } = "youshallnotpass";

        public uint Shards { get; set; } = 1;

        public bool SelfDeaf { get; set; }
    }
}
