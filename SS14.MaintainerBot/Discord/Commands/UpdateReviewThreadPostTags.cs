using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Discord.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.Commands;

public record UpdateReviewThreadPostTags(
    Guid ReviewThreadId,
    ulong GuildId,
    IEnumerable<string> GithubLabels,
    MaintainerReviewStatus ProcessStatus,
    PullRequestStatus PullRequestStatus
    ) : ICommand<DiscordMessage?>;