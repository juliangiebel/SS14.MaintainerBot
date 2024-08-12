using Octokit;
using SS14.MaintainerBot.Core.Models.Entities;

namespace SS14.MaintainerBot.Discord.Types;

public class ProcessPostTemplateModel
{
    public PullRequest PullRequest { get; init; }
    public MergeProcess MergeProcess { get; init; }
    
    public ProcessPostTemplateModel(PullRequest pullRequest, MergeProcess mergeProcess)
    {
        PullRequest = pullRequest;
        MergeProcess = mergeProcess;
    }
}