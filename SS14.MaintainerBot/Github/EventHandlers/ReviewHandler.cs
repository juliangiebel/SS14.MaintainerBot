using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Github.EventHandlers;

[UsedImplicitly]
public class ReviewHandler : IEventHandler<ReviewEvent>
{
    public Task HandleAsync(ReviewEvent eventModel, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}