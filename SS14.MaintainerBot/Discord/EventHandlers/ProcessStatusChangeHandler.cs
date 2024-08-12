using Discord;
using Discord.WebSocket;
using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Entities;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Github.Events;

namespace SS14.MaintainerBot.Discord.EventHandlers;

public class ProcessStatusChangeHandler: IEventHandler<MergeProcessStatusChangedEvent>
{
    private readonly DiscordConfiguration _configuration = new();
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClientService _client;
    
    public ProcessStatusChangeHandler(IServiceScopeFactory scopeFactory, IConfiguration configuration, DiscordClientService client)
    {
        _scopeFactory = scopeFactory;
        _client = client;
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

           // TODO: Change this to create a new forum post when there is none already
           if (message == null)
               continue;

           var thread = await _client.GetThread(message.GuildId, message.ChannelId, message.MessageId);
           if (!thread.HasValue)
            return;

           // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
           switch (eventModel.MergeProcess.Status)
           {
               case MergeProcessStatus.NotStarted:
               case MergeProcessStatus.Interrupted:
                   await HandleUnscheduled(thread.Value, eventModel.MergeProcess);
                   break;
               
               case MergeProcessStatus.Scheduled:
                   await HandleScheduled(thread.Value, eventModel.MergeProcess);
                   break;
               
               default:
                   await HandleFinal(thread.Value, eventModel.MergeProcess);
                   break;
           }
       }
       
       
       /*var previousButton = (ButtonComponent) socketModal
            .Message.Components.First()
            .Components.First(c => c.CustomId == StopMergeButton);

        var button = new ButtonBuilder(previousButton).WithDisabled(true);

        await socketModal.Message.ModifyAsync(p =>
        {
            p.Components = new ComponentBuilder().WithButton(button).Build();
        });*/
    }
    
    /// <summary>
    /// Gets called when the merge process has the `scheduled` status and can be interrupted
    /// </summary>
    private async Task HandleScheduled((SocketThreadChannel channel, IMessage message) thread, MergeProcess process)
    {
        return;
    }
    
    /// <summary>
    /// Gets called when the merge process is in a state that can be set to scheduled. Like `Unscheduled` or `Interrupted`
    /// </summary>
    private async Task HandleUnscheduled((SocketThreadChannel channel, IMessage message) thread, MergeProcess process)
    {
        return;
    }
    
    /// <summary>
    /// Gets called when the merge process entered a state that can't be changed back from. Like `Merged` or `Failed`
    /// </summary>
    private async Task HandleFinal((SocketThreadChannel channel, IMessage message) thread, MergeProcess process)
    {
        return;
    }
}