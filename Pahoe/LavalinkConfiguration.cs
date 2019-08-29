using System;

namespace Pahoe
{
    // TODO: more refined validation.
    public sealed class LavalinkConfiguration
    {
        public string Address
        {
            get => _address;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value));

                _address = value;
            }
        }
        private string _address = "127.0.0.1";

        public int Port
        {
            get => _port;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _port = value;
            }
        }
        private int _port = 2333;

        public string Authorization { get; set; } = "youshallnotpass";

        public int Shards
        {
            get => _shards;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _shards = value;
            }
        }
        private int _shards = 1;

        public bool SelfDeaf { get; set; }
    }
}
