using System;
using System.IO;
using System.Buffers;
using System.Text.Json;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal ref struct PayloadWriter
    {
        public Utf8JsonWriter Writer { get; private set; }

        private ClientWebSocket _webSocket;
        private MemoryStream _stream;
        private byte[] _buffer;

        internal PayloadWriter(ClientWebSocket webSocket, int bufferSize = 1024)
        {
            _webSocket = webSocket;
            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            _stream = new MemoryStream(_buffer, 0, bufferSize);
            Writer = new Utf8JsonWriter(_stream);
        }

        internal void WriteStartPayload(string op, string guildId)
        {
            Writer.WriteStartObject();
            Writer.WriteString("op", op);
            Writer.WriteString("guildId", guildId);
        }

        internal ValueTask SendAsync()
        {
            Writer.WriteEndObject();
            Writer.Flush();
            return _webSocket.SendAsync(_buffer.AsMemory().Slice(0, (int) Writer.BytesCommitted), WebSocketMessageType.Text, true, default);
        }

        public void Dispose()
        {
            if (Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}
