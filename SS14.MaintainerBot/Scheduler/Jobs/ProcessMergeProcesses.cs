using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Octokit;
using Quartz;
using Serilog;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models;
using SS14.MaintainerBot.Models.Types;
using ILogger = Serilog.ILogger;

namespace SS14.MaintainerBot.Scheduler.Jobs;

[CronSchedule("Scheduler#MergeProcessCron", "MergeProcesses", "processing", true)]
public class ProcessMergeProcesses : IJob
{
    private readonly IGithubApiService _apiService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _log;
    private readonly Context _dbContext;
    
    private readonly GithubBotConfiguration _configuration = new();
    
    public ProcessMergeProcesses(IServiceScopeFactory scopeFactory, IConfiguration configuration, IGithubApiService apiService, Context dbContext)
    {
        _apiService = apiService;
        _dbContext = dbContext;
        _scopeFactory = scopeFactory;
        
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        _log = Log.ForContext<ProcessMergeProcesses>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _log.Debug("Processing merge processes");
        var fireTime = context.FireTimeUtc.UtcDateTime;

        //using var scope = _scopeFactory.CreateScope();
        //var dbContext = scope.Resolve<Context>();
        
        var processes = await _dbContext.MergeProcesses!
            .Include(p => p.PullRequest)
            .Where(p => p.Status == MergeProcessStatus.Scheduled && p.StartedOn + p.MergeDelay < fireTime)
            .ToListAsync();

        foreach (var process in processes)
        {
            var installation = new InstallationIdentifier(
                process.PullRequest.InstallationId,
                process.PullRequest.GhRepoId);

            var ghPullRequest = await _apiService.GetPullRequest(installation, process.PullRequest.Number);
            if (ghPullRequest == null || ghPullRequest.Mergeable == false)
            {
                var command = new ChangeMergeProcessStatus(
                    installation,
                    process.PullRequest.Number,
                    ghPullRequest == null ? MergeProcessStatus.Failed : MergeProcessStatus.Interrupted);

                await command.ExecuteAsync();
                continue;
            }
            
            await _apiService.MergePullRequest(
                installation,
                process.PullRequest.Number,
                _configuration.MergeMethod);
        }
        
        if (processes.Count > 0)
            _log.Information("Merged {count} pull requests", processes.Count);
    }
}