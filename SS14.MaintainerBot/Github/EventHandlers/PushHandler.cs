using FastEndpoints;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Github.EventHandlers;

public class PushHandler : IEventHandler<PushEvent>
{
    public Task HandleAsync(PushEvent eventModel, CancellationToken ct)
    {
        // TODO: A push should stop the merge workflow if present
        throw new NotImplementedException();
    }
}