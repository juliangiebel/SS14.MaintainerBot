﻿using FastEndpoints;
using JetBrains.Annotations;
using Serilog;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github;

[UsedImplicitly]
public sealed class GithubCommandHandler :
    ICommandHandler<CreateOrUpdateComment, PullRequestComment?>,
    ICommandHandler<MergePullRequest, bool>,
    ICommandHandler<CreateMergeProcess, MergeProcess?>,
    ICommandHandler<ChangeMergeProcessStatus, MergeProcess?>,
    ICommandHandler<SavePullRequest, PullRequest?>,
    ICommandHandler<GetPullRequest, PullRequest?>,
    ICommandHandler<GetPullRequests, List<PullRequest>>

{
    private readonly IGithubApiService _githubApiService;
    private readonly GithubBotConfiguration _configuration = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public GithubCommandHandler(
        IGithubApiService githubApiService, 
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        _githubApiService = githubApiService;
        _scopeFactory = scopeFactory;
    }

    public async Task<PullRequestComment?> ExecuteAsync(CreateOrUpdateComment command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        PullRequestComment? comment;
        var comments = await dbRepository.GetCommentsOfType(command.PullRequestId, command.Type, ct);

        if (comments.Count > 0)
        {
            comment = await UpdateComment(comments.Last(), command, ct);
        }
        else
        {
            comment = await CreateComment(command, ct);
        }
        
        await dbRepository.DbContext.SaveChangesAsync(ct);
        return comment;
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

        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        await dbRepository.DbContext.PullRequestComment!.AddAsync(comment, ct);
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
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        var mergeProcessRepository = scope.Resolve<MergeProcessRepository>();
        
        var pullRequest = await dbRepository.GetPullRequest(command.Installation.RepositoryId, command.PullRequestNumber, ct);
        if (pullRequest == null)
            return null;
        
        var mergeProcess = await mergeProcessRepository.CreateMergeProcessForPr(
            pullRequest,
            command.Status,
            command.MergeDelay,
            ct);

        await dbRepository.DbContext.SaveChangesAsync(ct);

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
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        var mergeProcessRepository = scope.Resolve<MergeProcessRepository>();
        
        var mergeProcess = await mergeProcessRepository.SetMergeProcessStatusForPr(
            command.Installation.RepositoryId, 
            command.PullRequestNumber, 
            command.Status, 
            ct
        );

        await dbRepository.DbContext.SaveChangesAsync(ct);
        
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

    public async Task<PullRequest?> ExecuteAsync(SavePullRequest command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        var pullRequest = await dbRepository.GetPullRequest(command.Installation.RepositoryId, command.Number, ct);
        if (pullRequest is not null and not {Status: PullRequestStatus.Closed})
            return null;
        
        pullRequest ??= new PullRequest
        {
            InstallationId = command.Installation.InstallationId,
            GhRepoId = command.Installation.RepositoryId,
            Number = command.Number,
            Status = PullRequestStatus.Open
        };

        dbRepository.DbContext.PullRequest!.Update(pullRequest);
        await dbRepository.DbContext.SaveChangesAsync(ct);

        return pullRequest;
    }

    public async Task<PullRequest?> ExecuteAsync(GetPullRequest command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        return await dbRepository.GetPullRequest(command.Installation.RepositoryId, command.Number, ct);
    }

    public async Task<List<PullRequest>> ExecuteAsync(GetPullRequests command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<GithubDbRepository>();
        
        return await dbRepository.GetPullRequests(command.Installation.RepositoryId, ct);
    }
}