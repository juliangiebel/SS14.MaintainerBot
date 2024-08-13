using Discord;
using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Discord.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.Commands;

public record UpdateMergeProcessPost(
    DiscordMessage Message,
    InstallationIdentifier Installation,
    MergeProcess MergeProcess,
    int PullRequestNumber,
    MessageComponent? Button
    ) : ICommand;