using FastEndpoints;
using Octokit;

namespace SS14.MaintainerBot.Github.Events;

public record ReviewEvent(PullRequestReviewEventPayload Payload) : IEvent;