using FastEndpoints;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Discourse.EventHandlers;

public class ProcessStatusChangeHandler : IEventHandler<ReviewThreadStatusChangedEvent>
{
    public async Task HandleAsync(ReviewThreadStatusChangedEvent eventModel, CancellationToken ct)
    {
        // TODO: Discourse wawa
    }
}