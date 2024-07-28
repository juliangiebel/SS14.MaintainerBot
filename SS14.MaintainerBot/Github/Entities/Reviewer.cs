using System.ComponentModel.DataAnnotations;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Entities;

public class Reviewer
{
    [Required]
    public Guid PullRequestId { get; set; }
    [Required]
    public long GhUserId { get; set; }
    [Required]
    public ReviewStatus Status { get; set; }
}