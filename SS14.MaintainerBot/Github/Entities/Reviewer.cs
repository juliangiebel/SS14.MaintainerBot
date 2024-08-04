using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Entities;

[PrimaryKey("PullRequestId", "GhUserId")]
public class Reviewer
{ 
    public Guid PullRequestId { get; set; }
    public long GhUserId { get; set; }
    [Required]
    public ReviewStatus Status { get; set; }
    [Required, MaxLength(800)]
    public string Name { get; set; } = string.Empty;
}