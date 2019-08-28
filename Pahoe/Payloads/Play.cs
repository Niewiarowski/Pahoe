using System;
using System.Text.Json;
using System.Threading.Tasks;
using Pahoe.Search;

namespace Pahoe.Payloads
{
    internal static class Play
    {
        internal static ValueTask SendAsync(LavalinkPlayer player, LavalinkTrack track, TimeSpan startTime = default, TimeSpan endTime = default, bool noReplace = false)
        {
            using PayloadWriter payloadWriter = new PayloadWriter(player.Client.WebSocket);
            Utf8JsonWriter writer = payloadWriter.Writer;

            payloadWriter.WriteStartPayload("play", player.GuildIdStr);
            writer.WriteString("track", track.Hash);
            if (startTime != default)
                writer.WriteString("startTime", ((int)startTime.TotalMilliseconds).ToString());
            if (endTime != default)
                writer.WriteString("endTime", ((int)endTime.TotalMilliseconds).ToString());
            writer.WriteBoolean("noReplace", noReplace);

            return payloadWriter.SendAsync();
        }
    }
}
