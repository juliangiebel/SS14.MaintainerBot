using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Discord.Entities;

namespace SS14.MaintainerBot.Discord;

public class DiscordDbRepository
{
    public readonly Context DbContext;

    public DiscordDbRepository(Context dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<DiscordMessage?> GetMessage(ulong guildId, ulong messageId, CancellationToken ct)
    {
        return await DbContext.DiscordMessage!
            .Where(m => m.GuildId == guildId && m.MessageId == messageId)
            .SingleOrDefaultAsync(ct);
    }
}