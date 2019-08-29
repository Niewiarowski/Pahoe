using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal ref struct PayloadWriter
    {
        public readonly Utf8JsonWriter Writer;

        private readonly LavalinkPlayer _player;
        private readonly byte[] _buffer;
        private readonly MemoryStream _stream;

        internal PayloadWriter(LavalinkPlayer player, int bufferSize = 1024)
        {
            _player = player;
            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            _stream = new MemoryStream(_buffer, 0, bufferSize);
            Writer = new Utf8JsonWriter(_stream);
        }

        internal void WriteStartPayload(string op)
        {
            Writer.WriteStartObject();
            Writer.WriteString("op", op);
            Writer.WriteString("guildId", _player.GuildIdStr);
        }

        internal ValueTask SendAsync()
        {
            Writer.WriteEndObject();
            Writer.Flush();
            return _player.Client.WebSocket.SendAsync(_buffer.AsMemory().Slice(0, (int) Writer.BytesCommitted), WebSocketMessageType.Text, true, default);
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            Writer.Dispose();
        }
    }
}
