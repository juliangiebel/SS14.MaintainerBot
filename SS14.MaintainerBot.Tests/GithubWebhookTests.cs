using FastEndpoints;
using FastEndpoints.Testing;
using FluentAssertions;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;
using SS14.MaintainerBot.Models.Types;

namespace SS14.MaintainerBot.Tests;

public class GithubWebhookTests(MaintainerBotFixture fixture) : TestBase<MaintainerBotFixture>
{
    
    [Fact]
    public async Task PrMatchingRequirementsGetsProcessed()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.OpenPullRequestEvent(number, fixture.OpenPullRequest);
        await prOpenedEvent.PublishAsync();
        var payload = prOpenedEvent.Payload;
        var pullRequest = await fixture.GithubDbRepository.GetPullRequest(
            payload.Repository.Id, 
            payload.Number,
            new CancellationToken());

        pullRequest.Should().NotBeNull();
        pullRequest!.Status.Should().Be(PullRequestStatus.Open);
        pullRequest.Comments.Should().HaveCount(1);
        pullRequest.Comments.First().CommentType.Should().Be(PrCommentType.Introduction);
    }

    [Fact]
    public async Task ApprovalsUnderLimitDoesntStartWorkflow()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.OpenPullRequestEvent(number, fixture.OpenPullRequest);
        await prOpenedEvent.PublishAsync();

        var reviewEvent = fixture.ReviewEvent(number, Fake.Random.Int(), fixture.MaintainerApproved);
        await reviewEvent.PublishAsync();
        
        var payload = prOpenedEvent.Payload;
        var pullRequest = await fixture.GithubDbRepository.GetPullRequest(
            payload.Repository.Id, 
            payload.Number,
            new CancellationToken());
        
        pullRequest.Should().NotBeNull();
        pullRequest!.Status.Should().Be(PullRequestStatus.Open);
        pullRequest.Comments.Should().HaveCount(1);
        pullRequest.Reviewers.Should().HaveCount(1);

        var mergeProcess = await fixture.GithubDbRepository.GetMergeProcessForPr(pullRequest.Id, new CancellationToken());

        mergeProcess.Should().NotBeNull();
        mergeProcess!.Status.Should().Be(MergeProcessStatus.NotStarted);
    }
    
    [Fact]
    public async Task EnoughApprovalsStartsWorkflow()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.OpenPullRequestEvent(number, fixture.OpenPullRequest);
        await prOpenedEvent.PublishAsync();

        var reviewEvent1 = fixture.ReviewEvent(number, Fake.Random.Int(), fixture.MaintainerApproved);
        await reviewEvent1.PublishAsync();
        
        var reviewEvent2 = fixture.ReviewEvent(number, Fake.Random.Int(), fixture.MaintainerApproved);
        await reviewEvent2.PublishAsync();
        
        var payload = prOpenedEvent.Payload;
        var pullRequest = await fixture.GithubDbRepository.GetPullRequest(
            payload.Repository.Id, 
            payload.Number,
            new CancellationToken());
        
        pullRequest.Should().NotBeNull();
        pullRequest!.Reviewers.Should().HaveCount(2);

        var mergeProcess = await fixture.GithubDbRepository.GetMergeProcessForPr(pullRequest.Id, new CancellationToken());

        mergeProcess.Should().NotBeNull();
        mergeProcess!.Status.Should().Be(MergeProcessStatus.Scheduled);
    }
}