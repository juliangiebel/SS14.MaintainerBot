using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SS14.MaintainerBot.Github.Entities;

[Index("InstallationId", "GhRepoId", "Number")]
public class PullRequest
{
    [Key]
    public Guid Id {get; set;}
    
    [Required]
    public long InstallationId { get; set; }
    
    [Required]
    public long GhRepoId { get; set; }
    
    [Required] 
    public int Number { get; set; }

    [Required]
    public int Approvals { get; set; } = 0;

    [Required]
    public PullRequestStatus Status { get; set; } = PullRequestStatus.Open;
    
    public List<PullRequestComment> Comments { get; } = [];
}