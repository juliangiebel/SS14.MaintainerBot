using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Helpers;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class ReviewHandler : IEventHandler<ReviewEvent>
{
    private const string DismissedAction = "dismissed";

    private readonly PrVerificationService _verificationService;

    public ReviewHandler(PrVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    public async Task HandleAsync(ReviewEvent eventModel, CancellationToken ct)
    {
        if (eventModel.Action == DismissedAction)
            return;
        

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (eventModel.Review.State.Value)
        {
            case PullRequestReviewState.Approved: await OnPrApproved(eventModel, ct); break;
            case PullRequestReviewState.ChangesRequested: await OnPrChangesRequested(eventModel.Review, ct); break;
        }
    }

    private async Task OnPrApproved(ReviewEvent eventModel, CancellationToken ct)
    {
        if (!_verificationService.CheckGeneralRequirements(eventModel.PullRequest))
            return;
        
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