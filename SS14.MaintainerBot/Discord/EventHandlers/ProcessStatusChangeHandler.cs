using Discord;
using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Discord.Entities;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Discord.EventHandlers;

[UsedImplicitly]
public class ProcessStatusChangeHandler: IEventHandler<MergeProcessStatusChangedEvent>
{
    private readonly DiscordConfiguration _configuration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGithubApiService _githubApiService;
    public ProcessStatusChangeHandler(
        IServiceScopeFactory scopeFactory, 
        IConfiguration configuration, IGithubApiService githubApiService)
    {
        _scopeFactory = scopeFactory;
        _githubApiService = githubApiService;
        configuration.Bind(DiscordConfiguration.Name, _configuration);
    }

    public async Task HandleAsync(MergeProcessStatusChangedEvent eventModel, CancellationToken ct)
    {
        var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();

       foreach (var (id, guildConfig) in _configuration.Guilds)
       {
           if (!guildConfig.CheckInstallation(eventModel.Installation))
               continue;
               
           var message = await dbRepository.GetMessageFromProcess(id, eventModel.MergeProcess.Id , ct);
           if (message == null && !guildConfig.CreatePostBeforeApproval && eventModel.MergeProcess.Status == MergeProcessStatus.NotStarted)
               continue;

           var button = eventModel.MergeProcess.Status switch
           {
               MergeProcessStatus.NotStarted => BuildInterruptButton(true),
               MergeProcessStatus.Interrupted => BuildInterruptButton(true),
               MergeProcessStatus.Scheduled => BuildInterruptButton(false),
               _ => null
           };
           
           if (message == null)
           {
               await CreatePost(id, eventModel, button, ct);
           }
           else
           {
               await UpdatePost(eventModel, button, message, ct);
           }

           var githubPullRequest = await _githubApiService.GetPullRequest(eventModel.Installation, eventModel.PullRequestNumber);

           if (githubPullRequest == null) 
               continue;

           var pullRequest = await new GetPullRequest(eventModel.Installation, eventModel.PullRequestNumber).ExecuteAsync(ct);
           if (pullRequest == null)
            return;


           var updateTagsCommand = new UpdateMergeProcessPostTags(
               eventModel.MergeProcess.Id,
               id,
               githubPullRequest.Labels.Select(l => l.Name),
               eventModel.MergeProcess.Status,
               pullRequest.Status
           );

           await updateTagsCommand.ExecuteAsync(ct);
       }
    }
    
    private MessageComponent BuildInterruptButton(bool disabled)
    {
        var button = new ButtonBuilder()
            .WithLabel("Stop Merge")
            .WithCustomId(DiscordInteractionHandler.StopMergeButtonId)
            .WithStyle(ButtonStyle.Danger)
            .WithDisabled(disabled);
        
        return new ComponentBuilder().WithButton(button).Build();
    }
    
    private async Task CreatePost(ulong id, MergeProcessStatusChangedEvent eventModel, MessageComponent? button, CancellationToken ct)
    { 
        var command = new CreateMergeProcessPost(id, eventModel.Installation, eventModel.MergeProcess, eventModel.PullRequestNumber, button);
        await command.ExecuteAsync(ct);
    }
    
    
    private async Task UpdatePost(
        MergeProcessStatusChangedEvent eventModel, 
        MessageComponent? button,
        DiscordMessage message, 
        CancellationToken ct)
    {
        var command = new UpdateMergeProcessPost(message, eventModel.Installation, eventModel.MergeProcess, eventModel.PullRequestNumber, button);
        await command.ExecuteAsync(ct);
    }
}