using Octokit;
using SS14.GithubApiHelper.Exceptions;
using SS14.GithubApiHelper.Services;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Github.Types;
using ILogger = Serilog.ILogger;

namespace SS14.MaintainerBot.Github;

public interface IGithubApiService
{
    Task<long?> CreateCommentWithTemplate(InstallationIdentifier installation, int issueId, string templateName, object? model);
    Task UpdateCommentWithTemplate(InstallationIdentifier installation, int issueId, long commentId, string templateName, object? model);

    Task<bool> MergePullRequest(
        InstallationIdentifier installation,
        int pullRequestNumber,
        PullRequestMergeMethod mergeMethod,
        string? commitTitle = null,
        string? commitMessage = null
    );

    Task<CollaboratorPermissions> GetUserPermissionForRepository(InstallationIdentifier installation, User user);

    /// <summary>
    /// Gets a list of all installations of this github app
    /// </summary>
    /// <remarks>
    /// Used for selecting which installation to configure in the administration for example.
    /// Refrain from iterating over all installations and creating clients for them. Use installation ids saved in the database instead.
    /// </remarks>
    /// <returns>List of installations</returns>
    Task<IReadOnlyList<Installation>> GetInstallations();

    /// <summary>
    /// Gets a list of repositories the installation with the given id has access to.
    /// </summary>
    /// <remarks>
    /// Used for configuration in the administration interface
    /// </remarks>
    /// <param name="installationId">The installation id</param>
    /// <returns>A list of repositories the app has access to</returns>
    Task<RepositoriesResponse> GetRepositories(long installationId);

    Task<PullRequest?> GetPullRequest(InstallationIdentifier installation, int pullRequestNumber);
}

public sealed class GithubApiService : AbstractGithubApiService, IGithubApiService
{
    private readonly GithubTemplateService _templateService;

    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly ILogger _log;
    
    public GithubApiService(IConfiguration configuration, RateLimiterService rateLimiter, GithubTemplateService templateService) 
        : base(configuration, rateLimiter)
    {
        _templateService = templateService;
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);
        _log = Log.ForContext<GithubApiService>();
    }
    
    public async Task<long?> CreateCommentWithTemplate(InstallationIdentifier installation, int issueId, string templateName, object? model)
    {
        if (!await CheckRateLimit(installation))
            return null;

        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var body = await _templateService.RenderTemplate(templateName, model, _serverConfiguration.Language);
        var comment = await client.Issue.Comment.Create(installation.RepositoryId, issueId, body);

        if (comment != null)
            return comment.Id;

        _log.Error("Failed to create comment on repository with id {Repo} and issue {IssueId}",
            installation.RepositoryId,
            $"#{issueId}");

        return null;
    }

    public async Task UpdateCommentWithTemplate(InstallationIdentifier installation, int issueId, long commentId, string templateName, object? model)
    {
        if (!await CheckRateLimit(installation))
            return;

        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var body = await _templateService.RenderTemplate(templateName, model, _serverConfiguration.Language);
        var comment = await client.Issue.Comment.Update(installation.RepositoryId, issueId, body);

        if (comment != null)
            return;

        _log.Error("Failed to create comment on repository with id {Repo} and issue {IssueId}",
            installation.RepositoryId,
            $"#{issueId}");
    }
    
    private async Task<bool> CheckRateLimit(InstallationIdentifier installation)
    {
        if (!Configuration.Enabled)
            return false;

        // TODO: Handle this properly instead of throwing an exception
        if (!await RateLimiter.Acquire(installation.RepositoryId))
            throw new RateLimitException($"Hit rate limit for repository with id: {installation.RepositoryId}");

        return true;
    }

    public async Task<bool> MergePullRequest(
        InstallationIdentifier installation,
        int pullRequestNumber,
        PullRequestMergeMethod mergeMethod,
        string? commitTitle = null,
        string? commitMessage = null
        )
    {
        if (!await CheckRateLimit(installation))
            return false;

        var mergePullRequest = new MergePullRequest
        {
            MergeMethod = mergeMethod,
            CommitTitle = commitTitle,
            CommitMessage = commitMessage
        };
        
        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var merge = await client.PullRequest.Merge(installation.RepositoryId, pullRequestNumber, mergePullRequest);
        
        return merge.Merged;
    }

    public async Task<CollaboratorPermissions> GetUserPermissionForRepository(InstallationIdentifier installation, User user)
    {
        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var response = await client.Repository.Collaborator.ReviewPermission(installation.RepositoryId, user.Login);
        return response.Collaborator.Permissions;
    }

    public async Task<PullRequest?> GetPullRequest(InstallationIdentifier installation, int pullRequestNumber)
    {
        // TODO: handle this better. At least add retry with exp. backoff
        if (!await CheckRateLimit(installation))
            return null;
        
        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        return await client.PullRequest.Get(installation.RepositoryId, pullRequestNumber);
    }
}