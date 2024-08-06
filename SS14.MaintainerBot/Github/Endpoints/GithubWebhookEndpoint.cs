using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using Octokit.Internal;
using SS14.GithubApiHelper.Helpers;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Helpers;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Endpoints;

[UsedImplicitly]
public class GithubWebhookEndpoint : EndpointWithoutRequest
{
    private const string GithubEventHeader = "x-github-event";
    private const string ReviewDismissedAction = "dismissed";
    
    private readonly IGithubApiService _githubApiService;
    
    private readonly IConfiguration _configuration;
    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly GithubBotConfiguration _botConfiguration = new();

    public GithubWebhookEndpoint(IGithubApiService githubApiService, IConfiguration configuration)
    {
        _githubApiService = githubApiService;
        _configuration = configuration;
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);
        configuration.Bind(GithubBotConfiguration.Name, _botConfiguration);
    }

    public override void Configure()
    {
        Post("/api/GithubWebhook");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        HttpContext.Request.EnableBuffering();
        
        if (!HttpContext.Request.Headers.TryGetValue(GithubEventHeader, out var eventName) ||
            !await GithubWebhookHelper.VerifyWebhook(HttpContext.Request, _configuration))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var json = await GithubWebhookHelper.RetrievePayload(HttpContext.Request);
        var serializer = new SimpleJsonSerializer();

        var activity = serializer.Deserialize<ActivityPayload>(json);
        var cloneUrl = activity.Repository?.CloneUrl ?? string.Empty;
        if (cloneUrl != string.Empty && !_botConfiguration.RepositoryUrl.Equals(cloneUrl))
        {
            AddError($"Instance not configured for repository: {cloneUrl}");
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        
        IEvent? githubEvent = eventName[0] switch
        {
            "pull_request" => await HandlePullRequest(json, serializer, ct),
            "pull_request_review" => await HandleReview(json, serializer, ct),
            _ => null
        };

        if (githubEvent == null)
        {
            Logger.LogDebug("Received unhandled github event: {event_name}", eventName[0]);
            
            await SendOkAsync(ct);
            return;
        }
        
        Logger.LogTrace("Handled github event: {event_name}", eventName[0]);
        await SendOkAsync(ct);
    }

    private async Task<PullRequestEvent?> HandlePullRequest(string json, SimpleJsonSerializer serializer, CancellationToken ct)
    {
        var payload = serializer.Deserialize<PullRequestEventPayload>(json);
        if (payload == null)
            return null;
            
        var githubEvent = new PullRequestEvent(payload);
        
        await PublishAsync(githubEvent, cancellation: ct);
        return githubEvent;
    }
    
    private async Task<ReviewEvent?> HandleReview(string json, SimpleJsonSerializer serializer, CancellationToken ct)
    {
        var payload = serializer.Deserialize<PullRequestReviewEventPayload>(json);
        if (payload == null)
            return null;
        
        var reviewEvent = new ReviewEvent(payload);
        if (reviewEvent.Payload.Action == ReviewDismissedAction)
            return null;
        
        if (_botConfiguration.RequiresOrgMembership && reviewEvent.Payload.Review.AuthorAssociation.Value != AuthorAssociation.Member)
            return null;

        var installation = new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id);
        var permissions = await _githubApiService.GetUserPermissionForRepository(installation, payload.Sender);
        
        if (!CheckPermissions(permissions, _botConfiguration.ReviewPermissions))
            return null;
        
        var excludedStates = new[]
        {
            PullRequestReviewState.Commented, 
            PullRequestReviewState.Dismissed, 
            PullRequestReviewState.Pending
        };

        var githubEvent = excludedStates.Contains(reviewEvent.Payload.Review.State.Val()) ? null : reviewEvent;

        if (githubEvent != null)
            await PublishAsync(githubEvent, cancellation: ct);

        return githubEvent;
    }

    private static bool CheckPermissions(CollaboratorPermissions permissions, List<GithubPermissions> allowedPermissions)
    {
        if (allowedPermissions.Contains(GithubPermissions.None))
            return true;
        
        if (allowedPermissions.Contains(GithubPermissions.Admin))
            return permissions.Admin;

        if (allowedPermissions.Contains(GithubPermissions.Maintain))
            return permissions.Maintain ?? false;

        if (allowedPermissions.Contains(GithubPermissions.Push))
            return permissions.Push;

        if (allowedPermissions.Contains(GithubPermissions.Triage))
            return permissions.Triage  ?? false;

        return allowedPermissions.Contains(GithubPermissions.Pull) && permissions.Pull;
    }
}