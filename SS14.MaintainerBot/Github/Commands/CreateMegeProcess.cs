using FastEndpoints;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Entities;
using SS14.MaintainerBot.Models.Types;

namespace SS14.MaintainerBot.Github.Commands;

public record CreateMergeProcess
(
    InstallationIdentifier Installation,
    int PullRequestNumber,
    MergeProcessStatus Status,
    TimeSpan MergeDelay
) : ICommand<MergeProcess?>;