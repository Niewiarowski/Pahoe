using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pahoe.Search
{
    internal ref struct TrackReader
    {
        private readonly Span<byte> _bytes;
        private int _position;

        public TrackReader(Span<byte> bytes)
        {
            _bytes = bytes;
            _position = 0;
        }

        public string ReadString()
        {
            var length = Read<short>();
            var result = Encoding.UTF8.GetString(_bytes.Slice(_position, length));
            _position += length;

            return result;
        }

        public T Read<T>() where T : struct
        {
            T result = default;
            var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref result, 1));
            _bytes.Slice(_position, bytes.Length).CopyTo(bytes);
            _position += bytes.Length;

            if (BitConverter.IsLittleEndian)
                bytes.Reverse();

            return result;
        }
    }
}