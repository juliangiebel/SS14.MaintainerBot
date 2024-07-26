using FastEndpoints;
using JetBrains.Annotations;
using Serilog;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Entities;

namespace SS14.MaintainerBot.Github;

[UsedImplicitly]
public sealed class GithubCommandHandler : 
    ICommandHandler<CreateOrUpdateComment, PullRequestComment?>, 
    ICommandHandler<GetPullRequest, Guid>,
    ICommandHandler<MergePullRequest, bool>,
    ICommandHandler<CreateMergeProcess, MergeProcess?>,
    ICommandHandler<ChangeMergeProcessStatus, MergeProcess?>
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

    public async Task<bool> ExecuteAsync(MergePullRequest command, CancellationToken ct)
    {
        return await _githubApiService.MergePullRequest(command.InstallationIdentifier, command.PullRequestNumber, mergeMethod: _configuration.MergeMethod);
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
        if (ct.IsCancellationRequested)
            return null;
        
        
        await _githubApiService.UpdateCommentWithTemplate(
            command.InstallationIdentifier,
            command.PullRequestNumber,
            lastComment.CommentId,
            templateName: command.TemplateName, 
            model: command.Model
        );
        
        return lastComment;
    }

    public async Task<MergeProcess?> ExecuteAsync(CreateMergeProcess command, CancellationToken ct)
    {
        var mergeProcess = await _dbRepository.CreateMergeProcessForPr(
            command.Installation.RepositoryId,
            command.PullRequestNumber,
            command.Status,
            command.MergeDelay,
            ct);

        await _dbRepository.DbContext.SaveChangesAsync(ct);

        if (mergeProcess == null)
            return null;
        
        var processEvent = new MergeProcessStatusChangedEvent(
            command.Installation,
            command.PullRequestNumber,
            mergeProcess
        );

        await processEvent.PublishAsync(Mode.WaitForNone, ct);
        
        return mergeProcess;
    }
    
    public async Task<MergeProcess?> ExecuteAsync(ChangeMergeProcessStatus command, CancellationToken ct)
    {
        var mergeProcess = await _dbRepository.SetMergeProcessStatusForPr(
            command.Installation.RepositoryId, 
            command.PullRequestNumber, 
            command.Status, 
            ct
        );

        await _dbRepository.DbContext.SaveChangesAsync(ct);
        
        if (mergeProcess is null)
        {
            Log.Error("Failed to change status of merge process for pull request: {RepoId}:{PrNumber}", 
                command.Installation.RepositoryId, command.PullRequestNumber);
            
            return null;
        }

        var statusChangedEvent = new MergeProcessStatusChangedEvent(
            command.Installation,
            command.PullRequestNumber,
            mergeProcess
            );

        await statusChangedEvent.PublishAsync(Mode.WaitForNone, cancellation: ct);
        
        return mergeProcess;
    }
}