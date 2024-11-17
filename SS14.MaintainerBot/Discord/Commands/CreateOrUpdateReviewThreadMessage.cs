using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.Commands;

public record CreateReviewThreadMessage(
    InstallationIdentifier Installation,
    ReviewThread ReviewThread,
    string Message
    ) : ICommand;