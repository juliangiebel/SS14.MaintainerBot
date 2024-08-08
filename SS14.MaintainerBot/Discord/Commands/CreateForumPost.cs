using FastEndpoints;

namespace SS14.MaintainerBot.Discord.Commands;

public record CreateForumPost (
    ulong GuildId,
    string Title
    ) : ICommand<Guid?>;