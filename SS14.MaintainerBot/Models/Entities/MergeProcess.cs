using System.ComponentModel.DataAnnotations;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Types;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SS14.MaintainerBot.Models.Entities;

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
}