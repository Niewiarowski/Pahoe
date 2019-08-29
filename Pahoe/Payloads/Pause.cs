using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Pause
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, bool pause)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("pause");

            writer.WriteBoolean("pause", pause);

            return payloadWriter.SendAsync();
        }
    }
}
