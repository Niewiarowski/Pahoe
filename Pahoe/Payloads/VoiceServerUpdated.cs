using System.Threading.Tasks;
using Disqord.Events;

namespace Pahoe.Payloads
{
    internal static class VoiceServerUpdated
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, VoiceServerUpdatedEventArgs e)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("voiceUpdate");

            writer.WriteString("sessionId", player.SessionId);

            writer.WriteStartObject("event");
            writer.WriteString("token", e.Token);
            writer.WriteString("guild_id", player.GuildIdStr);
            writer.WriteString("endpoint", e.Endpoint);
            writer.WriteEndObject();

            return payloadWriter.SendAsync();
        }
    }
}
