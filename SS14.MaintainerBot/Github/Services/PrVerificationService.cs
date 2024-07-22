using Octokit;

namespace SS14.MaintainerBot.Github.Helpers;

public class PrVerificationService
{
    private readonly GithubBotConfiguration _configuration;

    public PrVerificationService(IConfiguration configuration)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
    }

    public bool CheckGeneralRequirements(PullRequest pullRequest)
    {
        
    }
}