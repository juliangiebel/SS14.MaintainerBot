using FastEndpoints;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

// TODO: Implement proper result return type
public record MergePullRequest  
(
    InstallationIdentifier InstallationIdentifier,
    int PullRequestNumber
) : ICommand<bool>;