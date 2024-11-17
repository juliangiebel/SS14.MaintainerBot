using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Helpers;
using SS14.MaintainerBot.Github.Services;
using SS14.MaintainerBot.Github.Types;
using PullRequest = SS14.MaintainerBot.Github.Entities.PullRequest;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class PullRequestHandler : IEventHandler<PullRequestEvent>
{
    private readonly GithubBotConfiguration _configuration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PrVerificationService _verificationService;
    
    public PullRequestHandler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration, 
        PrVerificationService verificationService)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        _verificationService = verificationService;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(PullRequestEvent eventModel, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();

        var payload = eventModel.Payload;
        switch (payload.Action)
        {
            case "reopened": case "opened": await OnPullRequestOpened(payload, dbRepository, ct); break;
            case "closed": await OnPullRequestClosed(payload, dbRepository, ct); break;
            case "synced": await OnPullRequestSynced(payload, dbRepository, ct); break;
            case "labeled": await OnPullRequestLabeled(payload, dbRepository, ct); break;
        }
    }

    private async Task OnPullRequestLabeled(PullRequestEventPayload payload, GithubDbRepository dbRepository, CancellationToken ct)
    {
        if (payload.PullRequest.State.Value == ItemState.Closed)
            return;
        
        var labels = payload.PullRequest.Labels.Select(l => l.Name);
        if (!_configuration.InDiscussionLabels.Intersect(labels).Any())
            return;

        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        var installation = new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id);
        
        if (pullRequest is null)
        {
            var createPrCommand = new SavePullRequest(installation, payload.Number, payload.PullRequest.State.Parse<PullRequestStatus, ItemState>());
            pullRequest = await createPrCommand.ExecuteAsync(ct);
        }
        
        var command = new CreateOrUpdateReviewThread(
            installation,
            pullRequest!.Number,
            MaintainerReviewStatus.InDiscussion
        );
        
        await command.ExecuteAsync(ct);
        //await CheckForConflict(payload, pullRequest, ct);
    }

    /// <summary>
    /// Gets called when a commit got pushed to the remote branch of a PR
    /// </summary>
    private async Task OnPullRequestSynced(PullRequestEventPayload payload, GithubDbRepository dbRepository, CancellationToken ct)
    {
        /*var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;

        var changeStatusCommand = new ChangeReviewThreadStatus(
            new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
            pullRequest.Number,
            MaintainerReviewStatus.Interrupted
        );

        await changeStatusCommand.ExecuteAsync(ct);*/
    }

    private async Task OnPullRequestOpened(PullRequestEventPayload payload, GithubDbRepository dbRepository, CancellationToken ct)
    {
        if (!_verificationService.CheckGeneralRequirements(payload.PullRequest))
            return;

        var installation = new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id);
        var createPrCommand = new SavePullRequest(installation, payload.Number, payload.PullRequest.State.Parse<PullRequestStatus, ItemState>());
        var pullRequest = await createPrCommand.ExecuteAsync(ct);
        
        if (pullRequest == null)
            return;
        
        var processed = payload.PullRequest.Labels.Select(l => l.Name).Intersect(_configuration.InDiscussionLabels).Any()
                        || _configuration.CreateThreadForAllPrs 
                        && _verificationService.CheckProcessingRequirements(payload.PullRequest);
        
        if (payload.Action != "reopened" && _configuration.SendIntroductoryComment)
            await PostIntroductoryComment(pullRequest, processed, dbRepository, ct);
        
        if (!processed)
            return;

        var command = new CreateOrUpdateReviewThread(
            installation,
            pullRequest.Number,
            MaintainerReviewStatus.InDiscussion
        );

        await command.ExecuteAsync(ct);
        //await CheckForConflict(payload, pullRequest, ct);
    }
    
    private async Task OnPullRequestClosed(PullRequestEventPayload payload, GithubDbRepository dbRepository, CancellationToken ct)
    {
        var pullRequest = await dbRepository.GetPullRequest(payload.Repository.Id, payload.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;

        pullRequest.Status = PullRequestStatus.Closed;
        dbRepository.DbContext.PullRequest!.Update(pullRequest);
        await dbRepository.DbContext.SaveChangesAsync(ct);

        var changeStatusCommand = new ChangeReviewThreadStatus(
            new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
            pullRequest.Number,
            payload.PullRequest.Merged ? MaintainerReviewStatus.Merged : MaintainerReviewStatus.Closed
        );

        await changeStatusCommand.ExecuteAsync(ct);
    }
    
    private async Task PostIntroductoryComment(PullRequest pullRequest, bool isProcessed, GithubDbRepository dbRepository, CancellationToken ct)
    {
        var hasComment = await dbRepository.HasCommentsOfType(pullRequest.Id, PrCommentType.Introduction, ct);

        if (hasComment)
            return;

        var command = new CreateOrUpdateComment(
            new InstallationIdentifier(pullRequest.InstallationId, pullRequest.GhRepoId),
            pullRequest.Id,
            pullRequest.Number,
            _configuration.IntroductoryCommentTemplate,
            new { processed = isProcessed },
            PrCommentType.Introduction
        );

        await command.ExecuteAsync(ct);
    }

    private async Task CheckForConflict(PullRequestEventPayload payload, PullRequest pullRequest, CancellationToken ct)
    {
        if (payload.PullRequest.Mergeable != false)
            return;

        
        /*var changeStatusCommand = new ChangeReviewThreadStatus(
            new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
            pullRequest.Number,
            MaintainerReviewStatus.Interrupted
        );

        await changeStatusCommand.ExecuteAsync(ct);*/
    }
}