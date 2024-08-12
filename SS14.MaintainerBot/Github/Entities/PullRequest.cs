using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Github.Types;

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
    public PullRequestStatus Status { get; set; } = PullRequestStatus.Open;
    
    public List<PullRequestComment> Comments { get; } = [];
    public List<Reviewer> Reviewers { get; } = [];
}