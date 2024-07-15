using System.ComponentModel.DataAnnotations;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SS14.MaintainerBot.Github.Entities;

public class PullRequest
{
    [Key]
    public Guid Id {get; set;}
    
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