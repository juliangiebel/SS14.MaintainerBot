using FastEndpoints;
using SS14.MaintainerBot.Github.Entities;

namespace SS14.MaintainerBot.Github.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="InstallationIdentifier"></param>
/// <param name="PullRequestId"></param>
/// <param name="PullRequestNumber"></param>
/// <param name="TemplateName"></param>
/// <param name="Model"></param>
/// <param name="Type">The type of PR comment</param>
/// <param name="UpdateLastOfType">Whether to add a new comment or update the last of type</param>
public record CreateOrUpdateComment ( 
    InstallationIdentifier InstallationIdentifier,
    Guid PullRequestId,
    int PullRequestNumber,
    string TemplateName,
    object? Model,
    PrCommentType Type, 
    bool UpdateLastOfType = false
) : ICommand<PullRequestComment?>;