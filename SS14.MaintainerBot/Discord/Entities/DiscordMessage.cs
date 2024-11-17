using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Core.Models.Entities;

namespace SS14.MaintainerBot.Discord.Entities;

// TODO: Add unique index on guild id and message id
[PrimaryKey(nameof(GuildId), nameof(ChannelId), nameof(MessageId))]
public class DiscordMessage
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public Guid ReviewThreadId { get; set; }
    public ReviewThread ReviewThread { get; set; }
}