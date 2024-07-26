using System.Reflection;
using Quartz;
using Quartz.AspNetCore;
using Serilog;

namespace SS14.MaintainerBot.Scheduler;

public static class SchedulerExtension
{
    public static void AddScheduler(this IServiceCollection services)
    {
        services.AddQuartz();
        services.AddQuartzServer(q => q.WaitForJobsToComplete = true);
        services.AddScoped<IJobSchedulingService, JobSchedulingService>();
    }
    
    public static async void ScheduleMarkedJobs(this WebApplication app)
    {
        var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
        var log = Log.ForContext(typeof(SchedulerExtension));
        
        var jobTypes = from type in Assembly.GetExecutingAssembly().GetTypes()
            where type.IsDefined(typeof(CronScheduleAttribute), false)
            select type;

        var scheduler = await schedulerFactory.GetScheduler();

        foreach (var jobType in jobTypes)
        {
            var attribute =
                (CronScheduleAttribute) Attribute.GetCustomAttribute(jobType, typeof(CronScheduleAttribute))!;

            var cron = attribute.CronExpression;

            if (attribute.FromConfig)
            {
                var parts = cron.Split('#');
                if (parts.Length != 2)
                {
                    log.Error("Invalid config key: {Key}", cron);
                    continue;
                }
                
                cron = app.Configuration.GetSection(parts[0]).GetValue<string>(parts[1]);
                if (cron is null)
                    continue;
            }
                
            
            var job = JobBuilder.Create(jobType)
                .WithIdentity(attribute.Name, attribute.Group)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(attribute.Name + "-trigger", attribute.Group)
                .WithCronSchedule(cron)
                .ForJob(job)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            
            log.Information("Scheduled cron job {JobKey} with schedule {Schedule}", job.Key.ToString(), cron);
        }
    }
}