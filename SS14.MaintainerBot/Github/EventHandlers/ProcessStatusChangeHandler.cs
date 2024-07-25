using FastEndpoints;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Types;

namespace SS14.MaintainerBot.Github.EventHandlers;

public class ProcessStatusChangeHandler : IEventHandler<MergeProcessStatusChangedEvent>
{
    
    private readonly GithubBotConfiguration _configuration = new();

    private readonly IServiceScopeFactory _scopeFactory;
    
    public ProcessStatusChangeHandler(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(MergeProcessStatusChangedEvent eventModel, CancellationToken ct)
    {
        var template = eventModel.MergeProcess.Status switch
        {
            MergeProcessStatus.Scheduled => _configuration.MergeProcessStartedCommentTemplate,
            MergeProcessStatus.Merging => "merging", // TODO: merging template
            MergeProcessStatus.Merged => "merged", // TODO: merged template
            MergeProcessStatus.Interrupted => _configuration.MergeProcessStoppedCommentTemplate,
            MergeProcessStatus.Failed => "failed", // TODO: failed template
            MergeProcessStatus.Closed => _configuration.MergeProcessPrClosedCommentTemplate,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var command = new CreateOrUpdateComment(
            eventModel.Installation,
            eventModel.MergeProcess.PullRequestId,
            eventModel.PullRequestNumber,
            template,
            eventModel.MergeProcess,
            PrCommentType.Workflow,
            eventModel.MergeProcess.Status != MergeProcessStatus.Scheduled
        );

        await command.ExecuteAsync(ct);
    }
}