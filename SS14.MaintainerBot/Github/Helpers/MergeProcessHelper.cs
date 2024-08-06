using FastEndpoints;
using Serilog;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;
using PullRequest = SS14.MaintainerBot.Github.Entities.PullRequest;

namespace SS14.MaintainerBot.Github.Helpers;

public static class MergeProcessHelper
{
    public static async Task<MergeProcess?> angeProcessStatus(
        long repositoryId,
        GithubDbRepository dbRepository,
        PullRequest pullRequest,
        MergeProcessStatus status,
        string commentTemplate,
        CancellationToken ct)
    {
        var mergeProcess = await dbRepository.SetMergeProcessStatusForPr(
            repositoryId, 
            pullRequest.Number, 
            status, 
            ct
        );

        if (mergeProcess is null)
        {
            Log.Error("Failed to change status of merge process for pull request: {Repo}:{PrNumber}", repositoryId, pullRequest.Number);
            return null;
        }
        
        var command = new CreateOrUpdateComment(
            new InstallationIdentifier(pullRequest.InstallationId, pullRequest.GhRepoId),
            pullRequest.Id,
            pullRequest.Number,
            commentTemplate,
            mergeProcess,
            PrCommentType.Workflow,
            true
        );

        await command.ExecuteAsync(ct);
        return mergeProcess;
    }
}