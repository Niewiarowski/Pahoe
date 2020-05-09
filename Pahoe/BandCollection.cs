using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pahoe
{
    public sealed class BandCollection : IList<float>, IReadOnlyList<float>
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

        bool ICollection<float>.IsReadOnly => true;

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

        public int IndexOf(float item)
            => Array.IndexOf(Bands, item);

        public void Clear()
            => Array.Clear(Bands, 0, 15);

        public bool Contains(float item)
            => Array.IndexOf(Bands, item) != -1;

        public void CopyTo(float[] array, int arrayIndex)
            => Bands.CopyTo(array, arrayIndex);

        public IEnumerator<float> GetEnumerator()
        {
            for (var i = 0; i < 15; i++)
                yield return Bands[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        void IList<float>.Insert(int index, float item)
            => throw new NotSupportedException();

        void IList<float>.RemoveAt(int index)
            => throw new NotSupportedException();

        void ICollection<float>.Add(float item)
            => throw new NotSupportedException();

        bool ICollection<float>.Remove(float item)
            => throw new NotSupportedException();
    }
}
