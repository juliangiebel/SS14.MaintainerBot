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
        // TODO: handle status ChangeRequested in database
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();

        var payload = eventModel.Payload;
        var status = Enum.Parse<ReviewStatus>(payload.Review.State.Val().ToString());
        
        await dbRepository.UpdatePullRequestReviewers(
            payload.Repository.Id, 
            payload.PullRequest.Number,
            payload.Review.User.Id, 
            payload.Review.User.Login, // That is the name. The actual name property is null a lot of times 
            status, 
            ct);
        
        await dbRepository.DbContext.SaveChangesAsync(ct);
        
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (eventModel.Payload.Review.State.Val())
        {
            case PullRequestReviewState.Approved: await OnPrApproved(eventModel, dbRepository, ct); break;
            case PullRequestReviewState.ChangesRequested: await OnPrChangesRequested(eventModel, dbRepository, ct); break;
        }
    }

    private async Task OnPrApproved(ReviewEvent eventModel, GithubDbRepository dbRepository, CancellationToken ct)
    {
        var payload = eventModel.Payload;
        if (!_verificationService.CheckGeneralRequirements(payload.PullRequest))
            return;
        
        // TODO: Add logging for pull request not existing
        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed} or {Status: PullRequestStatus.Approved})
            return;

        var changeRequests = await dbRepository.ReviewCountByStatus(pullRequest.Id, ReviewStatus.ChangeRequested, ct);
        if (changeRequests > 0)
            return;
        
        var approvals = await dbRepository.ReviewCountByStatus(pullRequest.Id, ReviewStatus.Approved, ct);
        if (approvals < _configuration.RequiredApprovals)
            return;
        
        pullRequest.Status = PullRequestStatus.Approved;
        dbRepository.DbContext.Update(pullRequest);

        await dbRepository.DbContext.SaveChangesAsync(ct);
        
        var hasMergeProcess = await dbRepository.HasMergeProcessForPr(pullRequest.Id, ct);
        
        if (hasMergeProcess)
        {
            var command = new ChangeMergeProcessStatus(
                new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
                payload.PullRequest.Number,
                MergeProcessStatus.Scheduled
            );
            
            await command.ExecuteAsync(ct);
        }
        else
        {
            var command = new CreateMergeProcess(
                new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
                payload.PullRequest.Number,
                MergeProcessStatus.Scheduled,
                _configuration.MergeDelay
            );
            
            await command.ExecuteAsync(ct);
        }
    }
    
    private async Task OnPrChangesRequested(ReviewEvent eventModel, GithubDbRepository dbRepository, CancellationToken ct)
    {
        var payload = eventModel.Payload;
        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;
        
        // TODO: Set status ChangeRequested
        
        var changeStatusCommand = new ChangeMergeProcessStatus(
            new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
            pullRequest.Number,
            MergeProcessStatus.Interrupted
        );

        await changeStatusCommand.ExecuteAsync(ct);
    }
}