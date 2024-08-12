using Microsoft.EntityFrameworkCore;
using Serilog;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Core.Models;

public class MergeProcessRepository
{
    public readonly Context DbContext;

    public MergeProcessRepository(Context dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<MergeProcess?> CreateMergeProcessForPr(
        PullRequest pullRequest,
        MergeProcessStatus status, 
        TimeSpan mergeDelay,
        CancellationToken ct)
    {
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
    
    public async Task<MergeProcess?> GetMergeProcessForPr(InstallationIdentifier installation, int number, CancellationToken ct)
    {
        return await DbContext.MergeProcesses!
            .Include(mp => mp.PullRequest)
            .Where(mp => mp.PullRequest.InstallationId == installation.InstallationId 
                         && mp.PullRequest.GhRepoId == installation.RepositoryId 
                         && mp.PullRequest.Number == number)
            .SingleOrDefaultAsync(ct);
    }


    public async Task<int> CountMergeProcesses(InstallationIdentifier installation, CancellationToken ct, params MergeProcessStatus[] statuses)
    {
        return await DbContext.MergeProcesses!
            .Where(mp => statuses.Contains(mp.Status))
            .CountAsync(ct);
    }
}