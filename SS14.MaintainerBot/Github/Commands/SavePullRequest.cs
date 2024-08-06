using FastEndpoints;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

/// <summary>
/// Gets a pull request from the github api and saves it as unscheduled if the pull request isn't already present in the database
/// </summary>
/// <param name="Installation"></param>
/// <param name="Number"></param>
public record SavePullRequest(InstallationIdentifier Installation, int Number) : ICommand<PullRequest?>;