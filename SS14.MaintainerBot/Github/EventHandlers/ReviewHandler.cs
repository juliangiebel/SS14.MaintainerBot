using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Helpers;
using SS14.MaintainerBot.Github.Services;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Types;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class ReviewHandler : IEventHandler<ReviewEvent>
{
    private const string DismissedAction = "dismissed";

    private readonly GithubBotConfiguration _configuration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PrVerificationService _verificationService;

    public ReviewHandler (
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        PrVerificationService verificationService)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);

        _scopeFactory = scopeFactory;
        _verificationService = verificationService;
    }

    public async Task HandleAsync(ReviewEvent eventModel, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        if (eventModel.Action == DismissedAction)
            return;
        

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (eventModel.Review.State.Value)
        {
            case PullRequestReviewState.Approved: await OnPrApproved(eventModel, dbRepository, ct); break;
            case PullRequestReviewState.ChangesRequested: await OnPrChangesRequested(eventModel, dbRepository, ct); break;
        }
    }

    private async Task OnPrApproved(ReviewEvent eventModel, GithubDbRepository dbRepository, CancellationToken ct)
    {
        if (!_verificationService.CheckGeneralRequirements(eventModel.PullRequest))
            return;
        
        // TODO: If configured to create messages and threads on approval instead of pr opening. Post them
        // TODO: Update PR in DB
        // TODO: Post or update PR comment
        // TODO: Start PR merge timer
        throw new NotImplementedException();
    }
    
    private async Task OnPrChangesRequested(ReviewEvent eventModel, GithubDbRepository dbRepository, CancellationToken ct)
    {
        var pullRequest = await dbRepository.TryGetPullRequest(eventModel.Repository.Id, eventModel.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;
        
        var changeStatusCommand = new ChangeMergeProcessStatus(
            new InstallationIdentifier(eventModel.Installation.Id, eventModel.Repository.Id),
            pullRequest.Number,
            MergeProcessStatus.Closed
        );

        await changeStatusCommand.ExecuteAsync(ct);
    }
}