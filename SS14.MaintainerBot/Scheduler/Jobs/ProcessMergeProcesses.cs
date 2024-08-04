using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models;

namespace SS14.MaintainerBot.Scheduler.Jobs;

[CronSchedule("Scheduler#MergeProcessCron", "MergeProcesses", "processing", true)]
public class ProcessMergeProcesses : IJob
{
    private readonly GithubApiService _apiService;
    private readonly Context _context;

    private GithubBotConfiguration _configuration = new();
    
    public ProcessMergeProcesses(IServiceScopeFactory scopeFactory, IConfiguration configuration, GithubApiService apiService)
    {
        _apiService = apiService;
        var scope = scopeFactory.CreateScope();
        _context = scope.Resolve<Context>();
        
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var fireTime = context.FireTimeUtc.DateTime;
        
        var processes = _context.MergeProcesses!
            .Include(p => p.PullRequest)
            .Where(p => p.StartedOn + p.MergeDelay < fireTime)
            .GetEnumerator();

        while (processes.MoveNext())
        {
            var installation = new InstallationIdentifier(
                processes.Current.PullRequest.InstallationId,
                processes.Current.PullRequest.GhRepoId);

            await _apiService.MergePullRequest(
                installation,
                processes.Current.PullRequest.Number,
                _configuration.MergeMethod);
        }
        
        processes.Dispose();
    }
}