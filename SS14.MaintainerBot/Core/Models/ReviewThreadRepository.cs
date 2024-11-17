using Microsoft.EntityFrameworkCore;
using Serilog;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Core.Models;

public class ReviewThreadRepository
{
    public readonly Context DbContext;

    public ReviewThreadRepository(Context dbContext)
    {
        DbContext = dbContext;
    }

    public ReviewThread CreateReviewThreadForPr(
        PullRequest pullRequest,
        MaintainerReviewStatus status)
    {
        var reviewThread = new ReviewThread
        {
            PullRequestId = pullRequest.Id,
            PullRequest = pullRequest,
            Status = status
        };
        
        DbContext.ReviewThread!.Add(reviewThread);
        return reviewThread;
    }

    public async Task<ReviewThread?> SetReviewProcessStatusForPr(long repositoryId, int pullRequestNumber, MaintainerReviewStatus status, CancellationToken ct)
    {
        var process = await DbContext.ReviewThread!
            .Where(mp => mp.PullRequest.GhRepoId == repositoryId && mp.PullRequest.Number == pullRequestNumber)
            .SingleOrDefaultAsync(ct);

        if (process == null)
            return null;

        process.Status = status;
        
        DbContext.Update(process);
        return process;
    }

    public async Task<bool> HasReviewThreadForPr(Guid pullRequestId, CancellationToken ct)
    {
        return await DbContext.ReviewThread!
            .Where(mp => mp.PullRequestId == pullRequestId)
            .AnyAsync(ct);
    }

    public async Task<ReviewThread?> GetReviewThreadForPr(Guid pullRequestId, CancellationToken ct)
    {
        return await DbContext.ReviewThread!
            .Where(mp => mp.PullRequestId == pullRequestId)
            .SingleOrDefaultAsync(ct);
    }
    
    public async Task<ReviewThread?> GetReviewThreadForPr(InstallationIdentifier installation, int number, CancellationToken ct)
    {
        return await DbContext.ReviewThread!
            .Include(mp => mp.PullRequest)
            .Where(mp => mp.PullRequest.InstallationId == installation.InstallationId 
                         && mp.PullRequest.GhRepoId == installation.RepositoryId 
                         && mp.PullRequest.Number == number)
            .SingleOrDefaultAsync(ct);
    }


    public async Task<int> CountReviewThreads(InstallationIdentifier installation, CancellationToken ct, params MaintainerReviewStatus[] statuses)
    {
        return await DbContext.ReviewThread!
            .Where(mp => statuses.Contains(mp.Status))
            .CountAsync(ct);
    }
}