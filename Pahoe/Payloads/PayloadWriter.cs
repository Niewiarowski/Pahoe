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
        private readonly ClientWebSocket _webSocket;
        private readonly byte[] _buffer;
        private readonly MemoryStream _stream;

        internal PayloadWriter(LavalinkPlayer player, int bufferSize = 1024) : this(player.Client.WebSocket, bufferSize)
        {
            _player = player;
        }

        internal PayloadWriter(ClientWebSocket webSocket, int bufferSize = 1024)
        {
            _player = null;
            _webSocket = webSocket;
            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            _stream = new MemoryStream(_buffer, 0, bufferSize);
            Writer = new Utf8JsonWriter(_stream);
        }

        internal void WriteStartPayload(string op)
        {
            Writer.WriteStartObject();
            Writer.WriteString("op", op);

            if (_player != null)
                Writer.WriteString("guildId", _player.GuildIdStr);
        }

        internal ValueTask SendAsync()
        {
            Writer.WriteEndObject();
            Writer.Flush();

            if (_webSocket.State == WebSocketState.Open)
                return _webSocket.SendAsync(_buffer.AsMemory().Slice(0, (int)Writer.BytesCommitted), WebSocketMessageType.Text, true, default);

            return default;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            Writer.Dispose();
        }
    }
}
