using Microsoft.EntityFrameworkCore;
using Serilog;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models;
using SS14.MaintainerBot.Models.Entities;
using SS14.MaintainerBot.Models.Types;

namespace SS14.MaintainerBot.Github;

public sealed class GithubDbRepository
{
    public readonly Context DbContext;

    public GithubDbRepository(Context context)
    {
        DbContext = context;
    }

    public async Task<PullRequest?> TryGetPullRequest(long ghRepoId, int number, CancellationToken ct)
    {
         return await DbContext.PullRequest!
             .Where(p => p.GhRepoId == ghRepoId && p.Number == number)
             .SingleOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<List<PullRequestComment>> GetCommentsOfType(Guid pullRequestId, PrCommentType type, CancellationToken ct)
    {
        return await DbContext.PullRequestComment!
            .Where(prc => prc.PullRequestId == pullRequestId && prc.CommentType == type)
            .ToListAsync(ct);
    }
    
    public async Task<bool> HasCommentsOfType(Guid pullRequestId, PrCommentType type, CancellationToken ct)
    {
        return await DbContext.PullRequestComment!
            .Where(prc => prc.PullRequestId == pullRequestId && prc.CommentType == type)
            .AnyAsync(ct);
    }

    public async Task<MergeProcess?> CreateMergeProcessForPr(
        long repositoryId, 
        int pullRequestNumber, 
        MergeProcessStatus status, 
        TimeSpan mergeDelay,
        CancellationToken ct)
    {
        var pullRequest = await TryGetPullRequest(repositoryId, pullRequestNumber, ct);
        if (pullRequest == null)
        {
            Log.Error("error");
            return null;
        }

        var mergeProcess = new MergeProcess
        {
            PullRequestId = pullRequest.Id,
            PullRequest = pullRequest,
            MergeDelay = mergeDelay,
            Status = status
        };

        DbContext.MergeProcesses!.Add(mergeProcess);
        return mergeProcess;
    }

    public async Task<MergeProcess?> SetMergeProcessStatusForPr(long repositoryId, int pullRequestNumber, MergeProcessStatus status, CancellationToken ct)
    {
        var process = await DbContext.MergeProcesses!
            .Where(mp => mp.PullRequest.GhRepoId == repositoryId && mp.PullRequest.Number == pullRequestNumber)
            .SingleOrDefaultAsync(ct);

        if (process == null)
            return null;

        process.Status = status;

        if (status == MergeProcessStatus.Scheduled)
            process.StartedOn = DateTime.UtcNow;
        
        DbContext.Update(process);
        return process;
    }

    public async Task<bool> HasMergeProcessForPr(Guid pullRequestId, CancellationToken ct)
    {
        return await DbContext.MergeProcesses!
            .Where(mp => mp.PullRequestId == pullRequestId)
            .AnyAsync(ct);
    }
}