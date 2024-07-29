using Quartz;

namespace SS14.MaintainerBot.Scheduler.Jobs;

[CronSchedule("Scheduler#MergeProcessCron", "MergeProcesses", "processing", true)]
public class ProcessMergeProcesses : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        
    }
}