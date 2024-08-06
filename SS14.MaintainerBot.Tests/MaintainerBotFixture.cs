using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Octokit;
using Octokit.Internal;
using SS14.MaintainerBot.Core.Helpers;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Events;
using SS14.MaintainerBot.Tests.MockServices;

namespace SS14.MaintainerBot.Tests;

public class MaintainerBotFixture : AppFixture<AssemblyMarker>
{ 
    private const string DatabaseName = "ss14_maintainer_test";

    public readonly SimpleJsonSerializer Serializer = new();
    public GithubDbRepository GithubDbRepository { get; private set; } = default!;


    public string OpenPullRequest { get; private set; } = default!;
    public string MaintainerApproved { get; private set; } = default!;
    public string MaintainerChangeRequested { get; private set; } = default!;
    public string MaintainerDismissed { get; private set; } = default!;
    
    public T GenerateWebhookPayload<T>(int number, int userId, string templateName)
    {
        var payload = templateName.Replace("%number%", number.ToString());
        payload = payload.Replace("%user_id%", userId.ToString());
        return Serializer.Deserialize<T>(payload);
    }

    public PullRequestEvent OpenPullRequestEvent(int number, string template)
    {
        var payload = GenerateWebhookPayload<PullRequestEventPayload>(number, 0, template);
        return new PullRequestEvent(payload);
    }

    public ReviewEvent ReviewEvent(int number, int userId, string template)
    {
        var payload = GenerateWebhookPayload<PullRequestReviewEventPayload>(number, userId, template);
        return new ReviewEvent(payload);
    }
    
    protected override void ConfigureServices(IServiceCollection s)
    {
        s.RemoveAll<IGithubApiService>();
        s.AddSingleton<IGithubApiService, GithubMock>();
        s.RemoveAll<DbContextOptions<Context>>();
        s.RemoveAll<Context>();
        s.AddDbContext<Context>(opt =>
        {
            opt.UseInMemoryDatabase(databaseName: DatabaseName);
        });
    }

    protected override async Task SetupAsync()
    {
        GithubDbRepository = Services.GetRequiredService<GithubDbRepository>();

        OpenPullRequest = await LoadWebhookTemplate("OpenPullRequestMatchingRequirements");
        MaintainerApproved = await LoadWebhookTemplate("MaintainerReviewApproved");
        MaintainerChangeRequested = await LoadWebhookTemplate("MaintainerReviewChange");
        MaintainerDismissed = await LoadWebhookTemplate("MaintainerChangeRequestDismissed");
    }

    private async Task<string> LoadWebhookTemplate(string template)
    {
        return await File.ReadAllTextAsync($"Resources/WebhookPayloads/Github/{template}.json");
    }
}