using FastEndpoints;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Discourse.EventHandlers;

public class ProcessStatusChangeHandler : IEventHandler<MergeProcessStatusChangedEvent>
{
    public async Task HandleAsync(MergeProcessStatusChangedEvent eventModel, CancellationToken ct)
    {
        // TODO: Discourse wawa
    }
}