using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Core.Models;
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
             .Include(p => p.Comments)
             .Where(p => p.GhRepoId == ghRepoId && p.Number == number)
             .AsSplitQuery()
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