
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
    public Dictionary<string, string> LabelTags { get; set; } = new();
    public Dictionary<PullRequestStatus, string>  StatusTags { get; set; } =  new();
    public Dictionary<MergeProcessStatus, string> ProcessTags { get; set; } = new();

    public bool CreatePostBeforeApproval { get; set; } = false;
    
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
}