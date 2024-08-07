﻿using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Core.Models.Entities;

namespace SS14.MaintainerBot.Discord.Entities;

[PrimaryKey(nameof(GuildId), nameof(MessageId))]
public class DiscordMessage
{
    public ulong GuildId { get; set; }
    public ulong MessageId { get; set; }
    public Guid MergeProcessId { get; set; }
    public MergeProcess MergeProcess { get; set; }
}