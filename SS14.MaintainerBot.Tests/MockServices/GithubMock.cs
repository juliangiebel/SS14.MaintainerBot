using Bogus;
using Octokit;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Tests.MockServices;

public class GithubMock : IGithubApiService
{
    private readonly Faker _faker = new();
    
    public Task<long?> CreateCommentWithTemplate(InstallationIdentifier installation, int issueId, string templateName, object? model)
    {
        return Task.FromResult((long?)Math.Abs(_faker.Random.Long()));
    }

    public Task UpdateCommentWithTemplate(InstallationIdentifier installation, int issueId, long commentId, string templateName,
        object? model)
    {
        return Task.CompletedTask;
    }

    public Task<bool> MergePullRequest(InstallationIdentifier installation, int pullRequestNumber, PullRequestMergeMethod mergeMethod,
        string? commitTitle = null, string? commitMessage = null)
    {
        return Task.FromResult(true);
    }

    public Task<CollaboratorPermissions> GetUserPermissionForRepository(InstallationIdentifier installation, User user)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Installation>> GetInstallations()
    {
        throw new NotImplementedException();
    }

    public Task<RepositoriesResponse> GetRepositories(long installationId)
    {
        throw new NotImplementedException();
    }
}