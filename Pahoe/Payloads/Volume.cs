using System.Text.Json;
using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Volume
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, ushort volume)
        {
            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("volume", player.GuildIdStr);
            writer.WriteNumber("volume", volume);

            return payloadWriter.SendAsync();
        }
    }
}
