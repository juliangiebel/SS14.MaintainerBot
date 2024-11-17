using Octokit;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github;

public sealed class GithubBotConfiguration
{
    public const string Name = "GithubBot";

    // old doc: Which permissions trigger or stop the merge workflow when submitting a review
    /// <summary>
    /// 
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
    public bool SendIntroductoryComment { get; set; } = false;

    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    public string IntroductoryCommentTemplate { get; set; } = "pr_intro";
    
    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    //public string MergeProcessStartedCommentTemplate { get; set; } = "process_started";
    
    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    //public string MergeProcessStoppedCommentTemplate { get; set; } = "process_stopped";

    //public string MergeProcessMergingCommentTemplate { get; set; } = "process_merging";
    //public string MergeProcessMergedCommentTemplate { get; set; } = "process_merged";
    //public string MergeProcessFailedCommentTemplate { get; set; } = "process_failed";
    
    /// <summary>
    /// The template to use for the introductory comment
    /// </summary>
    //public string MergeProcessPrClosedCommentTemplate { get; set; } = "process_closed";

    
    /// <summary>
    /// Whether to start a discourse thread and post a discord message when a PR gets oppened instead of first approval
    /// </summary>
    public bool CreateThreadForAllPrs { get; set; } = false;

    public List<string> InDiscussionLabels { get; set; } = [];
    
    public PullRequestMergeMethod MergeMethod { get; set; } = PullRequestMergeMethod.Squash;
    
    /// <summary>
    /// The amount of time a PR is left open after it gets scheduled for merging
    /// </summary>
    //public TimeSpan MergeDelay { get; set; } = TimeSpan.FromDays(2);

    /// <summary>
    /// The amount of approvals required before a PR gets scheduled for merging
    /// </summary>
    /// <remarks>
    /// The current implementation does not prevent one person approving multiple times from triggering the merge process
    /// </remarks>
    public int RequiredApprovals { get; set; } = 1;
}