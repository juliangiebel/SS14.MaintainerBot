using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class ReviewHandler : IEventHandler<ReviewEvent>
{
    private const string DismissedAction = "dismissed";

    public async Task HandleAsync(ReviewEvent eventModel, CancellationToken ct)
    {
        if (eventModel.Action == DismissedAction)
            return;
        

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (eventModel.Review.State.Value)
        {
            case PullRequestReviewState.Approved: await OnPrApproved(eventModel.Review, ct); break;
            case PullRequestReviewState.ChangesRequested: await OnPrChangesRequested(eventModel.Review, ct); break;
        }
    }

    private async Task OnPrApproved(PullRequestReview eventModelReview, CancellationToken ct)
    {
        
        // TODO: Check Pull request requirements
        // TODO: If configured to create messages and threads on approval instead of pr opening. Post them
        // TODO: Update PR in DB
        // TODO: Post or update PR comment
        // TODO: Start PR merge timer
        throw new NotImplementedException();
    }
    
    private async Task OnPrChangesRequested(PullRequestReview eventModelReview, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}