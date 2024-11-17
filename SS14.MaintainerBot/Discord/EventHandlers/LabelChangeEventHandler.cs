using FastEndpoints;
using Octokit;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.EventHandlers;

public class LabelChangeEventHandler : IEventHandler<PullRequestEvent>
{
    private readonly DiscordConfiguration _config = new();
    
    private readonly IServiceScopeFactory _scopeFactory;

    public LabelChangeEventHandler(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        configuration.Bind(DiscordConfiguration.Name, _config);
    }

    public async Task HandleAsync(PullRequestEvent eventModel, CancellationToken ct)
    {
        var payload = eventModel.Payload;
        switch (payload.Action)
        {
            case "unlabeled": case "labeled": await OnPullRequestLabeled(payload, ct); break;
        }
    }

    private async Task OnPullRequestLabeled(PullRequestEventPayload payload, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<ReviewThreadRepository>();
        
        var installation = new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id);
        
        foreach (var (id, guildConfig) in _config.Guilds)
        {
            if (!guildConfig.CheckInstallation(installation))
                continue;

            var reviewThread = await dbRepository.GetReviewThreadForPr(installation, payload.Number, ct);
            if (reviewThread == null)
                continue;

            var labels = payload.PullRequest.Labels.Select(l => l.Name);
            
            var tagsCommand = new UpdateReviewThreadPostTags(
                reviewThread.Id, 
                id, 
                labels,
                reviewThread.Status,
                reviewThread.PullRequest.Status);

            await tagsCommand.ExecuteAsync(ct);
            
            
        }
    }
}