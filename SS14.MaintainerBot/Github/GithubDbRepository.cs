using Microsoft.EntityFrameworkCore;
using Serilog;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github;

public sealed class GithubDbRepository
{
    public readonly Context DbContext;

    public GithubDbRepository(Context context)
    {
        DbContext = context;
    }

    public async Task<PullRequest?> GetPullRequest(long ghRepoId, int number, CancellationToken ct)
    {
         return await DbContext.PullRequest!
             .Include(p => p.Reviewers)
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
        var pullRequest = await GetPullRequest(repositoryId, pullRequestNumber, ct);
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

    public async Task<MergeProcess?> GetMergeProcessForPr(Guid pullRequestId, CancellationToken ct)
    {
        return await DbContext.MergeProcesses!
            .Where(mp => mp.PullRequestId == pullRequestId)
            .SingleOrDefaultAsync(ct);
    }
    
    public async Task UpdatePullRequestReviewers(long repositoryId, int pullRequestNumber, long userId, string userName, ReviewStatus status, CancellationToken ct)
    {
        if (status is ReviewStatus.Dismissed or ReviewStatus.Commented)
            return;
        
        var pullRequest = await DbContext.PullRequest!
            .Where(pr => pr.GhRepoId == repositoryId && pr.Number == pullRequestNumber)
            .SingleOrDefaultAsync(ct);

        if (pullRequest == null)
            return;

        var reviewer = await DbContext.Reviewer!
            .Where(r => r.PullRequestId == pullRequest.Id && r.GhUserId == userId)
            .SingleOrDefaultAsync(ct);

        var newReviewer = reviewer == null;
        
        reviewer ??= new Reviewer
        {
            PullRequestId = pullRequest.Id,
            GhUserId = userId,
            Name = userName
        };

        reviewer.Status = status;

        if (newReviewer)
        {
            DbContext.Add(reviewer);
        }
        else
        {
            DbContext.Update(reviewer);
        }
    }

    public async Task<int> ReviewCountByStatus(Guid pullRequestId, ReviewStatus status, CancellationToken ct)
    {
        return await DbContext.Reviewer!
            .Where(r => r.PullRequestId == pullRequestId && r.Status == status)
            .CountAsync(ct);
    }

    public async Task<List<PullRequest>> GetPullRequests(long repositoryId, CancellationToken ct)
    {
        return await DbContext.PullRequest!
            .Where(p => p.GhRepoId == repositoryId)
            .ToListAsync(ct);
    }
}