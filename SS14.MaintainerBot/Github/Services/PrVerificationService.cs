using Octokit;

namespace SS14.MaintainerBot.Github.Helpers;

public class PrVerificationService
{
    private readonly GithubBotConfiguration _configuration = new();

    public PrVerificationService(IConfiguration configuration)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
    }

    public bool CheckGeneralRequirements(PullRequest pullRequest)
    {

        // TODO: Implement checking configured requirements
        return true;
    }

    public bool CheckProcessingRequirements(PullRequest pullRequest)
    {
        // TODO: Implement checking configured processing requirements
        return true;
    }
}