using Discord;
using FastEndpoints;
using SS14.MaintainerBot.Discord.Entities;

namespace SS14.MaintainerBot.Discord.Commands;

public record CreateOrUpdateForumPost (
    Guid MergeProcessId,
    ulong GuildId,
    string Title,
    string Content,
    List<ButtonDefinition>? Buttons = null,
    List<string>? Tags = null
    ) : ICommand<DiscordMessage?>;
    
public record ButtonDefinition(string Title, string Id, ButtonStyle Style = ButtonStyle.Primary, bool Disabled = false);