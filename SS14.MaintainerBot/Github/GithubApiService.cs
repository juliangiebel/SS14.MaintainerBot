using SS14.GithubApiHelper.Exceptions;
using SS14.GithubApiHelper.Services;
using SS14.MaintainerBot.Configuration;
using ILogger = Serilog.ILogger;

namespace SS14.MaintainerBot.Github;

public class GithubApiService : AbstractGithubApiService
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
    
    private async Task<bool> CheckRateLimit(InstallationIdentifier installation)
    {
        if (!Configuration.Enabled)
            return false;

        // TODO: Handle this properly instead of throwing an exception
        if (!await RateLimiter.Acquire(installation.RepositoryId))
            throw new RateLimitException($"Hit rate limit for repository with id: {installation.RepositoryId}");

        return true;
    }
}