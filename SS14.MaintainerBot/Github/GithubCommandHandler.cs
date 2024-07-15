using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Github.Commands;

namespace SS14.MaintainerBot.Github;

[UsedImplicitly]
public class GithubCommandHandler : 
    ICommandHandler<CreateOrUpdateComment, Guid>, 
    ICommandHandler<GetPullRequest, Guid>, 
    ICommandHandler<MergePullRequest, Guid>,
    ICommandHandler<SavePullRequest, Guid>
{
    public Task<Guid> ExecuteAsync(CreateOrUpdateComment command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> ExecuteAsync(GetPullRequest command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> ExecuteAsync(MergePullRequest command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> ExecuteAsync(SavePullRequest command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}