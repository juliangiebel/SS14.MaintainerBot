﻿using Quartz;

namespace SS14.MaintainerBot.Scheduler;

public interface IJobSchedulingService
{
    Task RunJob<T>(string name, string group, JobDataMap? data) where T : IJob;
    IJobDetail CreateJob<T>(string name, string group, JobDataMap? data) where T : IJob;
    Task ScheduleJob(IJobDetail job, ITrigger trigger, IScheduler? scheduler = null!);
}