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
    
    public async Task<DiscordMessage?> GetMessageIncludingPr(ulong guildId, ulong messageId, CancellationToken ct)
    {
        return await DbContext.DiscordMessage!
            .Include(m => m.ReviewThread.PullRequest)
            .Where(m => m.GuildId == guildId && m.MessageId == messageId)
            .SingleOrDefaultAsync(ct);
    }
    
    
    public async Task<DiscordMessage?> GetMessageFromProcess(ulong guildId, Guid processId, CancellationToken ct)
    {
        return await DbContext.DiscordMessage!
            .Where(m => m.GuildId == guildId && m.ReviewThreadId == processId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task DeleteMessage(DiscordMessage message, CancellationToken ct)
    {
        DbContext.DiscordMessage!.Remove(message);
        await DbContext.SaveChangesAsync(ct);
    }
}