using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Events;

public record ReviewThreadStatusChangedEvent
(
    InstallationIdentifier Installation,
    int PullRequestNumber,
    ReviewThread ReviewThread
) : IEvent;