using System;
using System.Collections.Generic;
using System.Text;

namespace Pahoe
{
    public sealed class PahoeConfiguration
    {
        public string Address { get; set; } = "127.0.0.1";
        public uint Port { get; set; } = 2333;
        public string Password { get; set; } = "youshallnotpass";
        public uint Shards { get; set; } = 1;
        public bool SelfDeaf { get; set; }
    }
}
