using System.Text.Json;
using System.Threading.Tasks;

namespace Pahoe.Payloads
{
    internal static class Pause
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, bool pause)
        {
            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("pause", player.GuildIdStr);
            writer.WriteBoolean("pause", pause);

            return payloadWriter.SendAsync();
        }
    }
}
