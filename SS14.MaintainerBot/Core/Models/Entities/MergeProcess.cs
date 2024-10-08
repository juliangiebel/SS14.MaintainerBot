﻿using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SS14.MaintainerBot.Core.Models.Entities;

public class MergeProcess
{
    [Key]
    public Guid Id {get; set;}

    [Required]
    public PullRequest PullRequest { get; set; } = default!;
    
    public Guid PullRequestId { get; set; }

    [Required]
    public DateTime StartedOn { get; set; } = DateTime.UtcNow;
    
    [Required]
    public TimeSpan MergeDelay { get; set; }

    [Required]
    public MergeProcessStatus Status { get; set; } = MergeProcessStatus.Scheduled;

    [UsedImplicitly]
    public DateTime ScheduledOn => StartedOn.Add(MergeDelay);
}