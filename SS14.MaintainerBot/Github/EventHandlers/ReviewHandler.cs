using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Discord;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Types;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Helpers;
using SS14.MaintainerBot.Github.Services;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class ReviewHandler : IEventHandler<ReviewEvent>
{
    private readonly GithubBotConfiguration _configuration = new();
    private readonly ServerConfiguration _serverConfiguration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PrVerificationService _verificationService;
    private readonly DiscordTemplateService _templateService;

    public ReviewHandler (
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        PrVerificationService verificationService, 
        DiscordTemplateService templateService)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);

        _scopeFactory = scopeFactory;
        _verificationService = verificationService;
        _templateService = templateService;
    }

    public async Task HandleAsync(ReviewEvent eventModel, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        var reviewThreadRepository = scope.Resolve<ReviewThreadRepository>();

        var payload = eventModel.Payload;
        var status = Enum.Parse<ReviewStatus>(payload.Review.State.Val().ToString());

        if (status != ReviewStatus.Commented)
        {
            await dbRepository.UpdatePullRequestReviewers(
                payload.Repository.Id, 
                payload.PullRequest.Number,
                payload.Review.User.Id, 
                payload.Review.User.Login, // That is the name. The actual name property is null a lot of times 
                status, 
                ct);
        
            await dbRepository.DbContext.SaveChangesAsync(ct);
        }
        
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (eventModel.Payload.Review.State.Val())
        {
            case PullRequestReviewState.Approved: await OnPrApproved(eventModel, dbRepository, reviewThreadRepository, ct); break;
            case PullRequestReviewState.ChangesRequested: await OnPrChangesRequested(eventModel, dbRepository, reviewThreadRepository, ct); break;
            case PullRequestReviewState.Commented: await OnPrComment(eventModel, dbRepository, reviewThreadRepository, ct); break;
        }
    }

    private async Task OnPrComment(
        ReviewEvent eventModel, 
        GithubDbRepository dbRepository, 
        ReviewThreadRepository reviewThreadRepository, 
        CancellationToken ct)
    {
        var payload = eventModel.Payload;
        if (!_verificationService.CheckGeneralRequirements(payload.PullRequest))
            return;
        
        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;
        
        var reviewThread = await reviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, ct);

        if (reviewThread == null)
            return;
        
        var installation = new InstallationIdentifier(pullRequest.InstallationId, pullRequest.GhRepoId);
        await CreateReviewMessage(installation, reviewThread, eventModel, ct);
    }

    private async Task OnPrApproved(
        ReviewEvent eventModel, 
        GithubDbRepository dbRepository, 
        ReviewThreadRepository reviewThreadRepository, 
        CancellationToken ct)
    {
        var payload = eventModel.Payload;
        if (!_verificationService.CheckGeneralRequirements(payload.PullRequest))
            return;
        
        // TODO: Add logging for pull request not existing
        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed} or {Status: PullRequestStatus.Approved})
            return;

        var changeRequests = await dbRepository.ReviewCountByStatus(pullRequest.Id, ReviewStatus.ChangesRequested, ct);
        if (changeRequests > 0)
            return;
        
        var approvals = await dbRepository.ReviewCountByStatus(pullRequest.Id, ReviewStatus.Approved, ct);
        if (approvals < _configuration.RequiredApprovals)
            return;
        
        pullRequest.Status = PullRequestStatus.Approved;
        dbRepository.DbContext.Update(pullRequest);

        await dbRepository.DbContext.SaveChangesAsync(ct);

        var reviewThread = await reviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, ct);

        if (reviewThread == null)
            return;
        
        var installation = new InstallationIdentifier(pullRequest.InstallationId, pullRequest.GhRepoId);
        var statusEvent = new ReviewThreadStatusChangedEvent(
            installation, 
            pullRequest.Number,
            reviewThread
            );

        await statusEvent.PublishAsync(Mode.WaitForNone, ct);
        await CreateReviewMessage(installation, reviewThread, eventModel, ct);
        
        /*
        var hasReviewThread = await reviewThreadRepository.HasReviewThreadForPr(pullRequest.Id, ct);

        if (hasReviewThread)
        {
            var command = new ChangeReviewThreadStatus(
                new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
                payload.PullRequest.Number,
                MaintainerReviewStatus.Scheduled
            );

            await command.ExecuteAsync(ct);
        }
        else
        {
            var command = new CreateOrUpdateReviewThread(
                new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
                payload.PullRequest.Number,
                MaintainerReviewStatus.Scheduled,
                _configuration.MergeDelay
            );

            await command.ExecuteAsync(ct);
        }*/
    }

    private async Task OnPrChangesRequested(
        ReviewEvent eventModel, 
        GithubDbRepository dbRepository, 
        ReviewThreadRepository reviewThreadRepository, 
        CancellationToken ct)
    {
        var payload = eventModel.Payload;
        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;
        
        pullRequest.Status = PullRequestStatus.ChangesRequested;
        dbRepository.DbContext.Update(pullRequest);

        await dbRepository.DbContext.SaveChangesAsync(ct);

        var reviewThread = await reviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, ct);

        if (reviewThread == null)
            return;
        
        var statusEvent = new ReviewThreadStatusChangedEvent(
            new InstallationIdentifier(pullRequest.InstallationId, pullRequest.GhRepoId), 
            pullRequest.Number,
            reviewThread
        );

        await statusEvent.PublishAsync(Mode.WaitForNone, ct);
        
        var installation = new InstallationIdentifier(pullRequest.InstallationId, pullRequest.GhRepoId);
        await CreateReviewMessage(installation, reviewThread, eventModel, ct);
        
        /*var changeStatusCommand = new ChangeReviewThreadStatus(
            new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
            pullRequest.Number,
            MaintainerReviewStatus.Interrupted
        );

        await changeStatusCommand.ExecuteAsync(ct);*/
    }
    
    private async Task CreateReviewMessage(
        InstallationIdentifier installation, 
        ReviewThread reviewThread,
        ReviewEvent eventModel, 
        CancellationToken ct)
    {
        var review = eventModel.Payload.Review;
        var state = review.State.Val();
        if (eventModel.Payload.Action == "edited")
            return;
        
        var model = new ReviewPostModel(state.ToString(), review.User.Login, review.Body, review.HtmlUrl);
        var message = await _templateService.RenderTemplate("pr_review_post", model, _serverConfiguration.Language);
        var messageCommand = new CreateReviewThreadMessage(installation, reviewThread, message);
        await messageCommand.ExecuteAsync(ct);
    }
}