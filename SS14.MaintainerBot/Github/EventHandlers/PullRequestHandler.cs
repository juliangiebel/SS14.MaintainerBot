using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class PullRequestHandler : IEventHandler<PullRequestEvent>
{
    public async Task HandleAsync(PullRequestEvent eventModel, CancellationToken ct)
    {
        // TODO: edited?, labeled?, unlabeled?, reopened(same handler method as opened?)?,
        switch (eventModel.Action)
        {
            case "opened": await OnPullRequestOpened(eventModel, ct); break;
            case "closed": await OnPullRequestClosed(eventModel, ct); break;
        }
    }
    
    private async Task OnPullRequestOpened(PullRequestEvent eventModel, CancellationToken ct)
    {
        // TODO: Implement checking configured general pull request requirements
        
        // TODO: Store PR in DB
        
        // TODO: Send opening comment if configured
        
        // TODO: Check if pull request should be processed immediately (configurable) and check processing requirements.
        //  This should add to/replace the opening comment
        
        // TODO: If PR should be processed create a discourse thread and a discord message
    }
    
    private async Task OnPullRequestClosed(PullRequestEvent eventModel, CancellationToken ct)
    {
        // TODO: Check if PR is in database
        
        // TODO: Update database entry
        
        // TODO: Stop merge workflow if present
        
        // TODO: Update PR comment
        
        // TODO: Send discourse and discord message
    }
}