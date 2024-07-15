using FastEndpoints;
using Octokit;

namespace SS14.MaintainerBot.Github.Events;

public class PullRequestEvent : PullRequestEventPayload, IEvent;