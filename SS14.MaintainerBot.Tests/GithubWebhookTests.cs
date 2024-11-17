using FastEndpoints;
using FastEndpoints.Testing;
using FluentAssertions;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Tests;

public class GithubWebhookTests(MaintainerBotFixture fixture) : TestBase<MaintainerBotFixture>
{
    
    [Fact]
    public async Task PrMatchingRequirementsGetsProcessed()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.PullRequestEvent(number, fixture.OpenPullRequest);
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
    public async Task InDiscussionLabelCreatesReviewThread()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.PullRequestEvent(number, fixture.OpenPullRequest);
        await prOpenedEvent.PublishAsync();
        
        var labelAddedEvent = fixture.PullRequestEvent(number, fixture.PrLabeled);
        await labelAddedEvent.PublishAsync();
        
        var payload = prOpenedEvent.Payload;
        var pullRequest = await fixture.GithubDbRepository.GetPullRequest(
            payload.Repository.Id, 
            payload.Number,
            new CancellationToken());
        
        pullRequest.Should().NotBeNull();
        pullRequest!.Status.Should().Be(PullRequestStatus.Open);
        
        var reviewThread = await fixture.ReviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, new CancellationToken());

        reviewThread.Should().NotBeNull();
        reviewThread!.Status.Should().Be(MaintainerReviewStatus.InDiscussion);
    }

    [Fact]
    public async Task NewPullRequestsDontCreateReviewThread()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.PullRequestEvent(number, fixture.OpenPullRequest);
        await prOpenedEvent.PublishAsync();
        
        var payload = prOpenedEvent.Payload;
        var pullRequest = await fixture.GithubDbRepository.GetPullRequest(
            payload.Repository.Id, 
            payload.Number,
            new CancellationToken());
        
        pullRequest.Should().NotBeNull();
        pullRequest!.Status.Should().Be(PullRequestStatus.Open);
        
        var reviewThread = await fixture.ReviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, new CancellationToken());

        reviewThread.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingAPrConcludesDiscussionThread(bool merged)
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.PullRequestEvent(number, fixture.OpenPullRequest);
        await prOpenedEvent.PublishAsync();
        
        var labelAddedEvent = fixture.PullRequestEvent(number, fixture.PrLabeled);
        await labelAddedEvent.PublishAsync();
        
        var prClosedEvent = fixture.PullRequestEvent(number, merged ? fixture.PrMerged : fixture.PrClosed);
        await prClosedEvent.PublishAsync();
        
        var payload = prOpenedEvent.Payload;
        var pullRequest = await fixture.GithubDbRepository.GetPullRequest(
            payload.Repository.Id, 
            payload.Number,
            new CancellationToken());
        
        pullRequest.Should().NotBeNull();
        pullRequest!.Status.Should().Be(PullRequestStatus.Closed);
        
        var reviewThread = await fixture.ReviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, new CancellationToken());

        reviewThread.Should().NotBeNull();
        reviewThread!.Status.Should().Be(merged ? MaintainerReviewStatus.Merged : MaintainerReviewStatus.Closed);
    }
    
    /*[Fact]
    public async Task ApprovalsUnderLimitDoesntStartWorkflow()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.PullRequestEvent(number, fixture.OpenPullRequest);
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

        var mergeProcess = await fixture.ReviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, new CancellationToken());

        mergeProcess.Should().NotBeNull();
        mergeProcess!.Status.Should().Be(MaintainerReviewStatus.NotStarted);
    }
    
    [Fact]
    public async Task EnoughApprovalsStartsWorkflow()
    {
        var number = Fake.Random.Int();
        var prOpenedEvent = fixture.PullRequestEvent(number, fixture.OpenPullRequest);
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

        var mergeProcess = await fixture.ReviewThreadRepository.GetReviewThreadForPr(pullRequest.Id, new CancellationToken());

        mergeProcess.Should().NotBeNull();
        mergeProcess!.Status.Should().Be(MaintainerReviewStatus.Scheduled);
    }*/
}