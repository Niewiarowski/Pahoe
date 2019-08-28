using System.Text.Json;
using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Seek
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, uint positionMs)
        {
            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("seek", player.GuildIdStr);
            writer.WriteNumber("position", positionMs);

            return payloadWriter.SendAsync();
        }
    }
}
