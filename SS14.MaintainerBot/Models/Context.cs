using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Models.Entities;

namespace SS14.MaintainerBot.Models;

public class Context : DbContext
{
    public DbSet<MergeProcess>? MergeProcesses { get; set; }
    public DbSet<PullRequest>? PullRequest { get; set; }
    public DbSet<PullRequestComment>? PullRequestComment { get; set; }
    
    public DbSet<Reviewer>? Reviewer { get; set; }
    
    public Context(DbContextOptions<Context> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<MergeProcess>()
            .HasOne(e => e.PullRequest)
            .WithOne()
            .HasForeignKey<MergeProcess>(e => e.PullRequestId);
            
        builder.Entity<PullRequest>()
            .HasMany(e => e.Comments)
            .WithOne()
            .HasForeignKey(e => e.PullRequestId);
        
        builder.Entity<PullRequest>()
            .HasMany(e => e.Reviewers)
            .WithOne()
            .HasForeignKey(e => e.PullRequestId);

        builder.Entity<PullRequestComment>();
    }
}