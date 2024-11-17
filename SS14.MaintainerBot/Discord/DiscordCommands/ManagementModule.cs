using Discord;
using Discord.Interactions;
using FastEndpoints;
using SS14.GithubApiHelper.Services;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.DiscordCommands;

public class ManagementModule : InteractionModuleBase<SocketInteractionContext>
{
    
    private readonly DiscordConfiguration _config = new();
    private readonly ServerConfiguration _serverConfiguration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordTemplateService _templateService;
    private readonly IGithubApiService _githubApiService;
    
    public ManagementModule(IConfiguration configuration, IServiceScopeFactory scopeFactory, DiscordTemplateService templateService, IGithubApiService githubApiService)
    {
        _scopeFactory = scopeFactory;
        _templateService = templateService;
        _githubApiService = githubApiService;
        configuration.Bind(DiscordConfiguration.Name, _config);
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);
    }

    public override Task BeforeExecuteAsync(ICommandInfo command)
    {
        
        return Task.CompletedTask;
    }

    [SlashCommand("test", "Command for testing various discord related things during development")]
    public async Task TestCommand(
        [Summary(description: "The number of the pull request to test the discord integration with")] int number
        )
    {
        await DeferAsync(ephemeral: true);

        var guildConfig = _config.Guilds[Context.Guild.Id];
        var command = new ChangeReviewThreadStatus(
            new InstallationIdentifier(guildConfig.GithubInstallationId, guildConfig.GithubRepositoryId),
            number,
            MaintainerReviewStatus.InDiscussion);

        var result = await command.ExecuteAsync();
        await ModifyOriginalResponseAsync(p =>
        {
            p.Content = result != null
                ? $"Set process {result.Id} to in discussion."
                : "Failed to process status";
        });
    
    }

    [SlashCommand("reload-templates", "Reloads all liquid templates")]
    public async Task ReloadTemplates()
    {
        await DeferAsync();
        using var scope = _scopeFactory.CreateScope();

        var ghTemplateService = scope.Resolve<GithubTemplateService>();
        var discordTemplateService = scope.Resolve<DiscordTemplateService>();
        await ghTemplateService.LoadTemplates();
        await discordTemplateService.LoadTemplates();
        
        var content = await _templateService.RenderTemplate("status_response", culture: _serverConfiguration.Language);
        await ModifyOriginalResponseAsync(p => p.Content = content );
    }
    
    [SlashCommand("status", "Shows the bots status")]
    public async Task StatusCommand()
    {
        await DeferAsync();
        
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<ReviewThreadRepository>();
        
        var guildConfig = _config.Guilds[Context.Guild.Id];
        var installation = new InstallationIdentifier(guildConfig.GithubInstallationId, guildConfig.GithubRepositoryId);
        var ct = new CancellationToken();

        var inDiscussionCount = await dbRepository.CountReviewThreads(installation, ct, MaintainerReviewStatus.InDiscussion);
        var approvedCount = await dbRepository.CountReviewThreads(installation, ct, MaintainerReviewStatus.Approved);
        var rejectedCount = await dbRepository.CountReviewThreads(installation, ct, MaintainerReviewStatus.Rejected);

        var githubRepository = await _githubApiService.GetRepository(installation);
        
        var model = new StatusResponseModel
        {
            RepositoryName = githubRepository?.Name ?? "",
            RepositoryUrl = githubRepository?.HtmlUrl ?? "",
            InDiscussionCount = inDiscussionCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount
        };
        
        var content = await _templateService.RenderTemplate("status_response", model, _serverConfiguration.Language);
        await ModifyOriginalResponseAsync(p => p.Content = content );
    }

    private class StatusResponseModel
    {
        public string? RepositoryName { get; set; }
        public string? RepositoryUrl { get; set; }
        public int InDiscussionCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }
}