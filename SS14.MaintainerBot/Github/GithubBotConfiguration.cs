using Octokit;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github;

public sealed class GithubBotConfiguration
{
    public const string Name = "GithubBot";

    /// <summary>
    /// Which permissions trigger or stop the merge workflow when submitting a review
    /// </summary>
    public List<GithubPermissions> ReviewPermissions { get; set; } = [GithubPermissions.Maintain];

    /// <summary>
    /// Whether the triggering user needs to be a member of the organization owning the repository
    /// </summary>
    public bool RequiresOrgMembership { get; set; } = true;
    
    /// <summary>
    /// The url of the repository the bot handles. Other repositories will be ignored.
    /// </summary>
    /// <remarks>
    /// Multiple repository support can be added later when needed
    /// </remarks>
    public string RepositoryUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to post a comment on newly opened PRs.
    /// </summary>
    public bool SendIntroductoryComment { get; set; }

    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    public string IntroductoryCommentTemplate { get; set; } = "pr_intro.liquid";
    
    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    public string MergeProcessStartedCommentTemplate { get; set; } = "process_started.liquid";
    
    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    public string MergeProcessStoppedCommentTemplate { get; set; } = "process_stopped.liquid";

    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    public string MergeProcessPrClosedCommentTemplate { get; set; } = "process_closed.liquid";

    
    /// <summary>
    /// Whether to start a discourse thread and post a discord message when a PR gets oppened instead of first approval
    /// </summary>
    public bool ProcessUnapprovedPrs { get; set; } = true;

    public PullRequestMergeMethod MergeMethod { get; set; } = PullRequestMergeMethod.Squash;
}