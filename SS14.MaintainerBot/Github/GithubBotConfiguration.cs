namespace SS14.MaintainerBot.Github;

public class GithubBotConfiguration
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
}