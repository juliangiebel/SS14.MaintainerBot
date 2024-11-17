
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.Configuration;

public sealed class GuildConfiguration
{
    public long GithubInstallationId { get; set; }
    public long GithubRepositoryId { get; set; }
    public ulong ForumChannelId { get; set; }
    public List<ulong> MaintainerRoles { get; set; } = [];
    /// <summary>
    /// A dictionary of github labels to discord forum tags that gets applied to posts
    /// </summary>
    public Dictionary<string, string> LabelTags { get; set; } = new();
    
    /// <summary>
    /// The discord forum tags that should be applied for the different pull request states
    /// </summary>
    public Dictionary<PullRequestStatus, string>  StatusTags { get; set; } =  new();
    /// <summary>
    /// The discord forum tags that should be applied for the different maintainer review states
    /// </summary>
    public Dictionary<MaintainerReviewStatus, string> ProcessTags { get; set; } = new();
    
    /// <summary>
    /// A dictionary of any of the applicable tags that should also be applied as text labels in front of the post title
    /// </summary>
    public Dictionary<string, string> TitleTags { get; set; } = new();

    /// <summary>
    /// A list of tags that will cause the forum post to be archived when applied.
    /// Archived forum threads get deleted from the bots database
    /// </summary>
    public List<string> ArchivalTags { get; set; } = [];
    
    //public bool CreatePostBeforeApproval { get; set; } = false;
    
    public bool CheckInstallation(InstallationIdentifier installation)
    {
        return GithubInstallationId == installation.InstallationId && GithubRepositoryId == installation.RepositoryId;
    }

    public List<string> GetLabelTags(IEnumerable<string> labels)
    {
        return LabelTags
            .Where(tag => labels.Contains(tag.Key))
            .Select(tag => tag.Value)
            .ToList();
    }
    
    public List<string> GetTitleTags(IList<string> tags)
    {
        return TitleTags
            .Where(tag => tags.Contains(tag.Key))
            .Select(tag => tag.Value)
            .ToList();
    }
}