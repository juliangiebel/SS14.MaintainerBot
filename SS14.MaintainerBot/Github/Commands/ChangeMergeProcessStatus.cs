using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

public record ChangeMergeProcessStatus
(
    InstallationIdentifier Installation,
    int PullRequestNumber,
    MergeProcessStatus Status
): ICommand<MergeProcess?>;