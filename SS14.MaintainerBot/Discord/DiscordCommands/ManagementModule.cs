using Discord;
using Discord.Interactions;
using FastEndpoints;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Core.Models;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.DiscordCommands;

public class ManagementModule : InteractionModuleBase<SocketInteractionContext>
{
    
    private readonly DiscordConfiguration _config = new();
    private readonly ServerConfiguration _serverConfiguration = new();
    
    private MergeProcessRepository _dbRepository = default!;
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
        var scope = _scopeFactory.CreateScope();
        _dbRepository = scope.Resolve<MergeProcessRepository>();
        return Task.CompletedTask;
    }

    [SlashCommand("test", "Command for testing various discord related things during development")]
    public async Task TestCommand()
    {
        await DeferAsync(ephemeral: true);
    
        var command = new CreateForumPost(Context.Guild.Id, "Test Post");
        await command.ExecuteAsync();

        await ModifyOriginalResponseAsync(p => p.Content = "Done!");
    }
    
    [SlashCommand("status", "Shows the bots status")]
    public async Task StatusCommand()
    {
        await DeferAsync();

        var guildConfig = _config.Guilds[Context.Guild.Id];
        var installation = new InstallationIdentifier(guildConfig.GithubInstallationId, guildConfig.GithubRepositoryId);
        var ct = new CancellationToken();

        var failedCount = await _dbRepository.CountMergeProcesses(installation, ct, MergeProcessStatus.Failed);
        var scheduledCount = await _dbRepository.CountMergeProcesses(installation, ct, MergeProcessStatus.Scheduled);
        var unscheduledCount = await _dbRepository.CountMergeProcesses(installation, ct, MergeProcessStatus.NotStarted, MergeProcessStatus.Interrupted);

        var githubRepository = await _githubApiService.GetRepository(installation);
        
        var model = new StatusResponseModel
        {
            RepositoryName = githubRepository?.Name ?? "",
            RepositoryUrl = githubRepository?.Name ?? "",
            FailedCount = failedCount,
            ScheduledCount = scheduledCount,
            UnscheduledCount = unscheduledCount
        };
        
        var content = await _templateService.RenderTemplate("status_response", model, _serverConfiguration.Language);
        await ModifyOriginalResponseAsync(p => p.Content = content);
    }

    private class StatusResponseModel
    {
        public string? RepositoryName { get; set; }
        public string? RepositoryUrl { get; set; }
        public int FailedCount { get; set; }
        public int ScheduledCount { get; set; }
        public int UnscheduledCount { get; set; }
    }
}