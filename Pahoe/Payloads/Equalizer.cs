using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Equalizer
    {
        internal static ValueTask SendAsync(LavalinkPlayer player)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("equalizer");

            writer.WritePropertyName("bands");
            writer.WriteStartArray();
            var bands = player.Bands.Bands;
            var previousBands = player.Bands.PreviousBands;
            for (int i = 0; i < 15; i++)
            {
                float gain = bands[i];
                if (previousBands[i] != gain)
                {
                    previousBands[i] = gain;

                    writer.WriteStartObject();
                    writer.WriteNumber("band", i);
                    writer.WriteNumber("gain", gain);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();

            return payloadWriter.SendAsync();
        }
    }
}
