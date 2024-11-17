using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

public record ChangeReviewThreadStatus
(
    InstallationIdentifier Installation,
    int PullRequestNumber,
    MaintainerReviewStatus Status
): ICommand<ReviewThread?>;