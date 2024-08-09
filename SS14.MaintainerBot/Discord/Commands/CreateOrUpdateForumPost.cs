using Discord;
using FastEndpoints;
using SS14.MaintainerBot.Discord.Entities;

namespace SS14.MaintainerBot.Discord.Commands;

public record CreateForumPost (
    Guid MergeProcessId,
    ulong GuildId,
    string Title,
    string Content,
    List<ButtonDefinition>? Buttons = null
    ) : ICommand<DiscordMessage?>;
    
public record ButtonDefinition(string Title, string Id, ButtonStyle Style);