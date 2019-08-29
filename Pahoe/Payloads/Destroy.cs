using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Destroy
    {
        internal static ValueTask SendAsync(LavalinkPlayer player)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("destroy");

            return payloadWriter.SendAsync();
        }
    }
}
