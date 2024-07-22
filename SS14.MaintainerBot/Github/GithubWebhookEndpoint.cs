using FastEndpoints;
using JetBrains.Annotations;
using Octokit;
using Octokit.Internal;
using SS14.GithubApiHelper.Helpers;
using SS14.MaintainerBot.Configuration;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Github;

[UsedImplicitly]
public class GithubWebhookEndpoint : EndpointWithoutRequest
{
    private const string GithubEventHeader = "x-github-event";
    private const string ReviewDismissedAction = "dismissed";
    
    private readonly GithubApiService _githubApiService;
    
    private readonly IConfiguration _configuration;
    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly GithubBotConfiguration _botConfiguration = new();

    public GithubWebhookEndpoint(GithubApiService githubApiService, IConfiguration configuration)
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
            "pull_request" => serializer.Deserialize<PullRequestEvent>(json),
            "pull_request_review" => DeserializeReview(json, serializer),
            _ => null
        };

        if (githubEvent == null)
        {
            Logger.LogDebug("Received unhandled github event: {event_name}", eventName[0]);
            
            await SendOkAsync(ct);
            return;
        }
        
        Logger.LogTrace("Handled github event: {event_name}", eventName[0]);
        
        await PublishAsync(githubEvent, cancellation: ct);
        await SendOkAsync(ct);
    }

    private ReviewEvent? DeserializeReview(string json, SimpleJsonSerializer serializer)
    {
        var reviewEvent = serializer.Deserialize<ReviewEvent>(json);
        if (reviewEvent.Action == ReviewDismissedAction)
            return null;
        
        if (_botConfiguration.RequiresOrgMembership && reviewEvent.Review.AuthorAssociation.Value != AuthorAssociation.Member)
            return null;

        if (CheckPermissions(reviewEvent.Review.User.Permissions, _botConfiguration.ReviewPermissions))
            return null;
        
        var excludedStates = new[]
        {
            PullRequestReviewState.Commented, 
            PullRequestReviewState.Dismissed, 
            PullRequestReviewState.Pending
        };

        return excludedStates.Contains(reviewEvent.Review.State.Value) ? null : reviewEvent;
    }

    private static bool CheckPermissions(RepositoryPermissions permissions, List<GithubPermissions> allowedPermissions)
    {
        if (allowedPermissions.Contains(GithubPermissions.None))
            return true;
        
        if (allowedPermissions.Contains(GithubPermissions.Admin))
            return permissions.Admin;

        if (allowedPermissions.Contains(GithubPermissions.Maintain))
            return permissions.Maintain;

        if (allowedPermissions.Contains(GithubPermissions.Push))
            return permissions.Push;

        if (allowedPermissions.Contains(GithubPermissions.Triage))
            return permissions.Triage;

        return allowedPermissions.Contains(GithubPermissions.Pull) && permissions.Pull;
    }
}