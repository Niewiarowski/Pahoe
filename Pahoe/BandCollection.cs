using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pahoe
{
    public sealed class BandCollection : IReadOnlyList<float>
    {
        public int Count => 15;

        public float this[int index]
        {
            get
            {
                if (index < 0 || index > 14)
                    throw new ArgumentOutOfRangeException(nameof(index), "Band index must be between 0 and 14.");

                return Bands[index];
            }

            set
            {
                if (index < 0 || index > 14)
                    throw new ArgumentOutOfRangeException(nameof(index), "Band index must be between 0 and 14.");

                if (value < -0.25f || value > 1f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Band gain must be between -0.25 and 1.0.");

                Bands[index] = value;
            }
        }

        private readonly LavalinkPlayer _player;
        internal readonly float[] Bands;
        internal readonly float[] PreviousBands; // Used for not sending unnecessary JSON values.

        internal BandCollection(LavalinkPlayer player)
        {
            _player = player;
            Bands = new float[15];
            PreviousBands = new float[15];
        }

        public void SetBand(int index, float gain)
            => this[index] = gain;

        public void ResetBand(int index)
            => this[index] = 0f;

        public void MuteBand(int index)
            => this[index] = -0.25f;

        public ValueTask UpdateBandsAsync()
            => _player.UpdateBandsAsync();

        public IEnumerator<float> GetEnumerator()
        {
            for (var i = 0; i < 15; i++)
                yield return Bands[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
