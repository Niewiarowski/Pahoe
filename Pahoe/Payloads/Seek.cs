using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Seek
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, uint positionMs)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("seek");

            writer.WriteNumber("position", positionMs);

            return payloadWriter.SendAsync();
        }
    }
}
