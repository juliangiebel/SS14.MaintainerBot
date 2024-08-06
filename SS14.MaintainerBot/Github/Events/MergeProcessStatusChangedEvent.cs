using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Events;

public record MergeProcessStatusChangedEvent
(
    InstallationIdentifier Installation,
    int PullRequestNumber,
    MergeProcess MergeProcess
) : IEvent;