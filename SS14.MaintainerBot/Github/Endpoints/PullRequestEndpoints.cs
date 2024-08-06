using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Services;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Endpoints;


[UsedImplicitly]
public record PullRequestIdentifier(long InstallationId, long RepositoryId, int Number);

[UsedImplicitly]
[HttpGet("/api/{InstallationId}/{RepositoryId}/pr/{Number}")]
public class GetPullRequestEndpoint : Endpoint<PullRequestIdentifier, PullRequest?>
{
    public override async Task<PullRequest?> ExecuteAsync(PullRequestIdentifier req, CancellationToken ct)
    {
        var command = new GetPullRequest(
            new InstallationIdentifier(req.InstallationId, req.RepositoryId),
            req.Number
        );

        var pullRequest = await command.ExecuteAsync(ct);

        if (pullRequest != null) 
            return pullRequest;
        
        await SendNotFoundAsync(ct);
        return null;
    }
}

[UsedImplicitly]
[HttpGet("/api/{InstallationId}/{RepositoryId}/pr")]
public class GetPullRequestsEndpoint : Endpoint<InstallationIdentifier, List<PullRequest>>
{
    public override async Task<List<PullRequest>> ExecuteAsync(InstallationIdentifier req, CancellationToken ct)
    {
        return await new GetPullRequests(req).ExecuteAsync(ct);
    }
}

[UsedImplicitly]
[HttpPost("/api/{InstallationId}/{RepositoryId}/pr/{Number}/store")]
public class SavePullRequestEndpoint : Endpoint<PullRequestIdentifier, PullRequest?>
{
    private readonly PrVerificationService _verificationService;
    private readonly GithubApiService _githubApiService;
    
    public SavePullRequestEndpoint(PrVerificationService verificationService, GithubApiService githubApiService)
    {
        _verificationService = verificationService;
        _githubApiService = githubApiService;
    }

    public override async Task<PullRequest?> ExecuteAsync(PullRequestIdentifier req, CancellationToken ct)
    {
        var installation = new InstallationIdentifier(req.InstallationId, req.RepositoryId);
        var ghPullRequest = await _githubApiService.GetPullRequest(installation, req.Number);
        
        if (ghPullRequest == null || !_verificationService.CheckGeneralRequirements(ghPullRequest))
        {
            await SendErrorsAsync(ghPullRequest == null ? 404 : 400, cancellation: ct);
            return null;
        }
        
        var command = new SavePullRequest(
            new InstallationIdentifier(req.InstallationId, req.RepositoryId),
            req.Number
        );

        var pullRequest = await command.ExecuteAsync(ct);

        if (pullRequest != null) 
            return pullRequest;
        
        await SendNotFoundAsync(ct);
        return null;
    }
}
