using FastEndpoints;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Entities;
using SS14.MaintainerBot.Models.Types;

namespace SS14.MaintainerBot.Github.Events;

public record MergeProcessStatusChangedEvent
(
    InstallationIdentifier Installation,
    int PullRequestNumber,
    MergeProcess MergeProcess
) : IEvent;