using System.Threading.Tasks;
using Discord.WebSocket;

namespace Pahoe.Payloads
{
    internal static class VoiceServerUpdated
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, SocketVoiceServer voiceServer)
        {
            using var payloadWriter = new PayloadWriter(player);
            var writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("voiceUpdate");

            writer.WriteString("sessionId", player.SessionId);

            writer.WriteStartObject("event");
            writer.WriteString("token", voiceServer.Token);
            writer.WriteString("guild_id", player.GuildIdStr);
            writer.WriteString("endpoint", voiceServer.Endpoint);
            writer.WriteEndObject();

            return payloadWriter.SendAsync();
        }
    }
}
