using System.ComponentModel.DataAnnotations;
using SS14.MaintainerBot.Github.Entities;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SS14.MaintainerBot.Models.Entities;

public class MergeProcess
{
    [Key]
    public Guid Id {get; set;}

    [Required]
    public PullRequest PullRequest { get; set; } = default!;

    [Required]
    public DateTime Created { get; set; } = DateTime.Now;
}