using JetBrains.Annotations;

namespace SS14.MaintainerBot.Scheduler;

[UsedImplicitly]
public class SchedulerConfig
{
    public const string Name = "Scheduler";

    /// <summary>
    /// The cron schedule for checking merge processes and merging them when their merge delay has passed-
    /// </summary>
    /// <remarks>
    /// This happens at a fixed schedule to be able to batch merges using merge queues
    /// </remarks>
    /// <example>Daily at 2 am: <code>0 0 2 * * ?</code></example>
    public string MergeProcessCron { get; set; } = "0 0 2 * * ?";

}