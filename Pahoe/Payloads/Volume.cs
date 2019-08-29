using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Volume
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, ushort volume)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("volume");

            writer.WriteNumber("volume", volume);

            return payloadWriter.SendAsync();
        }
    }
}
