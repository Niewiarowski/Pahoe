using System.Text.Json;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Pahoe.Payloads
{
    internal static class VoiceServerUpdated
    {
        internal static ValueTask SendAsync(LavalinkClient client, SocketVoiceServer voiceServer)
        {
            if (!client.Players.TryGetValue(voiceServer.Guild.Id, out LavalinkPlayer player))
                return default;

            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("voiceUpdate", player.GuildIdStr);
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
