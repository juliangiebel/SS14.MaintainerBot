using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.EventHandlers;

public class ThreadStatusChangeHandler : IEventHandler<ReviewThreadStatusChangedEvent>
{
    
    private readonly GithubBotConfiguration _configuration = new();

    private readonly IServiceScopeFactory _scopeFactory;
    
    public ThreadStatusChangeHandler(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        configuration.Bind(GithubBotConfiguration.Name, _configuration);
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(ReviewThreadStatusChangedEvent eventModel, CancellationToken ct)
    {
        /*if (eventModel.ReviewThread.Status == MaintainerReviewStatus.NotStarted)
            return;
        
        var template = eventModel.ReviewThread.Status  switch
        {
            MaintainerReviewStatus.Scheduled => _configuration.MergeProcessStartedCommentTemplate,
            MaintainerReviewStatus.Merging => _configuration.MergeProcessMergingCommentTemplate,
            MaintainerReviewStatus.Merged => _configuration.MergeProcessMergedCommentTemplate,
            MaintainerReviewStatus.Interrupted => _configuration.MergeProcessStoppedCommentTemplate,
            MaintainerReviewStatus.Failed => _configuration.MergeProcessFailedCommentTemplate,
            MaintainerReviewStatus.Closed => _configuration.MergeProcessPrClosedCommentTemplate,
            MaintainerReviewStatus.NotStarted => "",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var command = new CreateOrUpdateComment(
            eventModel.Installation,
            eventModel.ReviewThread.PullRequestId,
            eventModel.PullRequestNumber,
            template,
            eventModel.ReviewThread,
            PrCommentType.Workflow,
            eventModel.ReviewThread.Status != MaintainerReviewStatus.Scheduled
        );

        await command.ExecuteAsync(ct);*/
    }
}