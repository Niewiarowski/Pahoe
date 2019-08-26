using System.Text.Json;
using System.Threading.Tasks;

namespace Pahoe.Payloads.Outgoing
{
    internal static class Destroy
    {
        internal static ValueTask SendAsync(LavalinkPlayer player)
        {
            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("destroy", player.GuildIdStr);

            return payloadWriter.SendAsync();
        }
    }
}
