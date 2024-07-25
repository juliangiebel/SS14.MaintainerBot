using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Helpers;
using PullRequest = SS14.MaintainerBot.Github.Entities.PullRequest;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class PullRequestHandler : IEventHandler<PullRequestEvent>
{
    private readonly GithubBotConfiguration _configuration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PrVerificationService _verificationService;
    private readonly GithubApiService _githubApiService;
    
    public PullRequestHandler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration, 
        PrVerificationService verificationService, 
        GithubDbRepository dbRepository, 
        GithubApiService githubApiService)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        _verificationService = verificationService;
        _githubApiService = githubApiService;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(PullRequestEvent eventModel, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        // TODO: edited?, labeled?, unlabeled?, reopened(same handler method as opened?)?,
        switch (eventModel.Action)
        {
            case "opened": await OnPullRequestOpened(eventModel, dbRepository, ct); break;
            case "closed": await OnPullRequestClosed(eventModel, dbRepository, ct); break;
            case "synced": await OnPullRequestSynced(eventModel, dbRepository, ct); break;
        }
    }

    /// <summary>
    /// Gets called when a commit got pushed to the remote branch of a PR
    /// </summary>
    /// <param name="eventModel"></param>
    /// <param name="dbRepository"></param>
    /// <param name="ct"></param>
    private async Task OnPullRequestSynced(PullRequestEvent eventModel, GithubDbRepository dbRepository,
        CancellationToken ct)
    {
        var pullRequest = await dbRepository.TryGetPullRequest(eventModel.Repository.Id, eventModel.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;
        
        // TODO: Interrupt workflow

    }

    private async Task OnPullRequestOpened(PullRequestEvent eventModel, GithubDbRepository dbRepository,
        CancellationToken ct)
    {
        if (!_verificationService.CheckGeneralRequirements(eventModel.PullRequest))
            return;

        var pullRequest = await dbRepository.TryGetPullRequest(eventModel.Repository.Id, eventModel.PullRequest.Number, ct);
        if (pullRequest is not null and not {Status: PullRequestStatus.Closed})
            return;
        
        pullRequest ??= new PullRequest
        {
            Approvals = 0,
            Number = eventModel.Number,
            GhRepoId = eventModel.Repository.Id,
        };
        
        pullRequest.Status = PullRequestStatus.Open;

        var processed = _configuration.ProcessUnapprovedPrs && _verificationService.CheckProcessingRequirements(eventModel.PullRequest);
        
        if (_configuration.SendIntroductoryComment)
            await PostIntroductoryComment(pullRequest, processed, dbRepository, ct);
        
        dbRepository.DbContext.PullRequest!.Update(pullRequest);
        await dbRepository.DbContext.SaveChangesAsync(ct);
        
        if (!processed)
            return;

        // new DiscordSendMessageCommand...
        // new DiscourseCreateThreadCommand...
        // TODO: If PR should be processed create a discourse thread and a discord message
    }
    
    private async Task OnPullRequestClosed(PullRequestEvent eventModel, GithubDbRepository dbRepository,
        CancellationToken ct)
    {
        var pullRequest = await dbRepository.TryGetPullRequest(eventModel.Repository.Id, eventModel.PullRequest.Number, ct);
        if (pullRequest is null or {Status: PullRequestStatus.Closed})
            return;

        pullRequest.Status = PullRequestStatus.Closed;
        dbRepository.DbContext.PullRequest!.Update(pullRequest);

        // TODO: Stop merge workflow if present

        // TODO: Update PR comment

        await dbRepository.DbContext.SaveChangesAsync(ct);

        // TODO: Send discourse and discord message
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
}