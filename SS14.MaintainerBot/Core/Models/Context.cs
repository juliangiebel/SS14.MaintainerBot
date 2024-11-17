using Microsoft.EntityFrameworkCore;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Discord.Entities;
using SS14.MaintainerBot.Github.Entities;

namespace SS14.MaintainerBot.Core.Models;

public class Context : DbContext
{
    public DbSet<ReviewThread>? ReviewThread { get; set; }
    public DbSet<PullRequest>? PullRequest { get; set; }
    public DbSet<PullRequestComment>? PullRequestComment { get; set; }
    
    public DbSet<Reviewer>? Reviewer { get; set; }
    
    public DbSet<DiscordMessage>? DiscordMessage { get; set; }
    
    public Context(DbContextOptions<Context> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ReviewThread>()
            .HasOne(e => e.PullRequest)
            .WithOne()
            .HasForeignKey<ReviewThread>(e => e.PullRequestId);

        builder.Entity<PullRequest>()
            .HasMany(e => e.Comments)
            .WithOne()
            .HasForeignKey(e => e.PullRequestId);

        builder.Entity<PullRequest>()
            .HasMany(e => e.Reviewers)
            .WithOne()
            .HasForeignKey(e => e.PullRequestId);

        builder.Entity<PullRequestComment>();

        builder.Entity<DiscordMessage>()
            .HasOne(m => m.ReviewThread)
            .WithOne()
            .HasForeignKey<DiscordMessage>(m => m.ReviewThreadId);
    }
}