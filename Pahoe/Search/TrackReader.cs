using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Pahoe.Search
{
    internal ref struct TrackReader
    {
        private readonly Span<byte> bytes;
        private int position;

        public TrackReader(Span<byte> bytes)
        {
            this.bytes = bytes;
            position = 0;
        }

        public string ReadString()
        {
            var length = Read<short>();
            int newPosition = position + length;

            string result = Encoding.UTF8.GetString(bytes.Slice(position, length));
            position = newPosition;

            return result;
        }

        public T Read<T>() where T : struct
        {
            T result = default;
            var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref result, 1));
            Read(bytes);
            if (BitConverter.IsLittleEndian)
                bytes.Reverse();

            return result;
        }

        private void Read(Span<byte> destination)
        {
            int newPosition = position + destination.Length;
            bytes.Slice(position, destination.Length).CopyTo(destination);
            position = newPosition;
        }
    }
}