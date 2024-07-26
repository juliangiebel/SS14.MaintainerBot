using JetBrains.Annotations;

namespace SS14.MaintainerBot.Scheduler;

[AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
public sealed class CronScheduleAttribute : Attribute
{
    public string CronExpression { get; }
    public string Name { get; }
    public string Group { get; }
    
    public bool FromConfig { get; }

    public CronScheduleAttribute(string cronExpression, string name, string group = "default", bool fromConfig = false)
    {
        CronExpression = cronExpression;
        Group = group;
        Name = name;
        FromConfig = fromConfig;
    }
}