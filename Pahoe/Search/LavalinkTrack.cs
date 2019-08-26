using System;
using System.Text;
using System.Buffers.Text;

namespace Pahoe.Search
{
    public struct LavalinkTrack
    {
        public string Hash { get; internal set; }
        public string Identifier { get; internal set; }
        public bool IsSeekable { get; internal set; }
        public string Author { get; internal set; }
        public TimeSpan Length { get; internal set; }
        public bool IsStream { get; internal set; }
        public string Title { get; internal set; }
        public string Uri { get; internal set; }

        public static LavalinkTrack Decode(string hash)
        {
            Span<byte> hashBuffer = stackalloc byte[hash.Length];
            Encoding.ASCII.GetBytes(hash, hashBuffer);
            Base64.DecodeFromUtf8InPlace(hashBuffer, out int bytesWritten);
            var reader = new TrackReader(hashBuffer.Slice(0, bytesWritten));

            if ((((reader.Read<int>() & 0xC0000000L) >> 30) & 1) != 0)
                reader.Read<sbyte>();

            return new LavalinkTrack
            {
                Hash = hash,
                Title = reader.ReadString(),
                Author = reader.ReadString(),
                Length = TimeSpan.FromMilliseconds(reader.Read<long>()),
                Identifier = reader.ReadString(),
                IsStream = reader.Read<bool>(),
                Uri = reader.Read<bool>() ? reader.ReadString() : string.Empty
            };
        }
    }

}
