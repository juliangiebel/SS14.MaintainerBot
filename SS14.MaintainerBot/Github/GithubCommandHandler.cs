using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Entities;

namespace SS14.MaintainerBot.Github;

[UsedImplicitly]
public sealed class GithubCommandHandler : 
    ICommandHandler<CreateOrUpdateComment, PullRequestComment?>, 
    ICommandHandler<GetPullRequest, Guid>, 
    ICommandHandler<MergePullRequest, Guid>,
    ICommandHandler<SavePullRequest, Guid>
{
    private readonly GithubApiService _githubApiService;
    private readonly GithubBotConfiguration _configuration;
    private readonly GithubDbRepository _dbRepository;

    public GithubCommandHandler(GithubApiService githubApiService, GithubBotConfiguration configuration, GithubDbRepository dbRepository)
    {
        _githubApiService = githubApiService;
        _configuration = configuration;
        _dbRepository = dbRepository;
    }

    public async Task<PullRequestComment?> ExecuteAsync(CreateOrUpdateComment command, CancellationToken ct)
    {
        PullRequestComment? comment;
        var comments = await _dbRepository.GetCommentsOfType(command.PullRequestId, command.Type, ct);

        if (comments.Count > 0)
        {
            comment = await UpdateComment(comments.Last(), command, ct);
        }
        else
        {
            comment = await CreateComment(command, ct);
        }
        
        await _dbRepository.DbContext.SaveChangesAsync(ct);
        return comment;
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

    private async Task<PullRequestComment?> CreateComment(CreateOrUpdateComment command, CancellationToken ct)
    {
        var id = await _githubApiService.CreateCommentWithTemplate(
            command.InstallationIdentifier,
            command.PullRequestNumber,
            command.TemplateName,
            command.Model
        );

        if (!id.HasValue)
            return null;
        
        var comment = new PullRequestComment
        {
            CommentId = id.Value,
            PullRequestId = command.PullRequestId,
            CommentType = PrCommentType.Introduction
        };

        await _dbRepository.DbContext.PullRequestComment!.AddAsync(comment, ct);
        return comment;
    }
    
    
    private async Task<PullRequestComment?> UpdateComment(
        PullRequestComment lastComment, 
        CreateOrUpdateComment command,
        CancellationToken ct)
    {
        return null;
    }
}