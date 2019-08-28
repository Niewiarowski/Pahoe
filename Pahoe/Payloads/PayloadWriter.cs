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
        private ClientWebSocket WebSocket { get; }
        private MemoryStream Stream { get; set;  }
        private byte[] Buffer { get; }

        internal PayloadWriter(ClientWebSocket webSocket, int bufferSize = 1024)
        {
            WebSocket = webSocket;
            Buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            Stream = new MemoryStream(Buffer, 0, bufferSize);
            Writer = new Utf8JsonWriter(Stream);
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
            return WebSocket.SendAsync(Buffer.AsMemory().Slice(0, (int)Writer.BytesCommitted), WebSocketMessageType.Text, true, default);
        }

        public void Dispose()
        {
            if(Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }

            if(Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}
