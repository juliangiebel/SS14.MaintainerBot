using FastEndpoints;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

// TODO: Implement proper result return type
/// <summary>
/// Merges the pull request with the given `PullRequestNumber`
/// </summary>
/// <param name="InstallationIdentifier">The github repository the PR resides in</param>
/// <param name="PullRequestNumber">The number of the pull request to merge</param>
public record MergePullRequest  
(
    InstallationIdentifier InstallationIdentifier,
    int PullRequestNumber
) : ICommand<bool>;