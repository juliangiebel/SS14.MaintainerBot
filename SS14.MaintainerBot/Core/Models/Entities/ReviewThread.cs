using System.ComponentModel.DataAnnotations;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SS14.MaintainerBot.Core.Models.Entities;

public class ReviewThread
{
    [Key]
    public Guid Id {get; set;}

    [Required]
    public PullRequest PullRequest { get; set; } = default!;
    
    public Guid PullRequestId { get; set; }

    [Required]
    public DateTime StartedOn { get; set; } = DateTime.UtcNow;

    [Required]
    public MaintainerReviewStatus Status { get; set; } = MaintainerReviewStatus.InDiscussion;
}