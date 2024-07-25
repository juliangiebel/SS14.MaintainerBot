using Octokit;
using SS14.GithubApiHelper.Exceptions;
using SS14.GithubApiHelper.Services;
using SS14.MaintainerBot.Configuration;
using SS14.MaintainerBot.Github.Types;
using ILogger = Serilog.ILogger;

namespace SS14.MaintainerBot.Github;

public sealed class GithubApiService : AbstractGithubApiService
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
}