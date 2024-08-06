using FastEndpoints;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

public record GetPullRequests(InstallationIdentifier Installation) : ICommand<List<PullRequest>>;
