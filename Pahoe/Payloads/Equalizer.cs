using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Equalizer
    {
        internal static ValueTask SendAsync(LavalinkPlayer player)
        {
            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("equalizer", player.GuildIdStr);

            writer.WritePropertyName("bands");
            writer.WriteStartArray();
            Span<float> bandsSpan = player.Bands.Span;
            for (int i = 0; i < 15; i++)
            {
                writer.WriteStartObject();
                writer.WriteNumber("band", i);
                writer.WriteNumber("gain", bandsSpan[i]);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            return payloadWriter.SendAsync();
        }
    }
}
