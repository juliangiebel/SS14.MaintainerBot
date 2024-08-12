using Discord;
using Discord.WebSocket;
using FastEndpoints;
using SS14.MaintainerBot.Core.Models.Types;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Github.Commands;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord;

public class DiscordInteractionHandler
{
    public const string StopMergeButton = "stop-merge";
    private const string StopMergeModal = "stop-merge-modal";
    private const string ReasonInput = "reason";
    
    private readonly DiscordConfiguration _config = new();

    private readonly DiscordClientService _clientService;
    private readonly IServiceScopeFactory _scopeFactory;

    public DiscordInteractionHandler(DiscordClientService clientService, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _clientService = clientService;
        _scopeFactory = scopeFactory;
        configuration.Bind(DiscordConfiguration.Name, _config);
    }

    public void Init()
    {
        _clientService.Client.ButtonExecuted += ButtonExecuted;
        _clientService.Client.ModalSubmitted += ModalSubmitted;
    }

    private async Task ModalSubmitted(SocketModal arg)
    {
        var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        switch (arg.Data.CustomId)
        {
            case StopMergeModal: await StopMergeSubmitted(dbRepository, arg); break;
        }
    }

    

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        //var scope = _scopeFactory.CreateScope();
        //var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        switch (arg.Data.CustomId)
        {
            case StopMergeButton: await StopMergePressed(arg); break;
        }
    }
    
    private async Task StopMergeSubmitted(DiscordDbRepository dbRepository, SocketModal socketModal)
    {
        if (!socketModal.GuildId.HasValue)
            return;
        
        var reason = socketModal.Data.Components.ToList().First().Value;
        await socketModal.RespondAsync($"{socketModal.User.Mention} canceled the automatic merge process.\n**Reason**\n{reason}");

        var message = await dbRepository.GetMessageIncludingPr(socketModal.GuildId!.Value, socketModal.Message.Id, new CancellationToken());

        if (message == null)
            return;

        var config = _config.Guilds[socketModal.GuildId.Value];
        var command = new ChangeMergeProcessStatus(
            new InstallationIdentifier(config.GithubInstallationId, config.GithubRepositoryId),
            message.MergeProcess.PullRequest.Number,
            MergeProcessStatus.Interrupted);

        await command.ExecuteAsync();

        /*var previousButton = (ButtonComponent) socketModal
            .Message.Components.First()
            .Components.First(c => c.CustomId == StopMergeButton);

        var button = new ButtonBuilder(previousButton).WithDisabled(true);

        await socketModal.Message.ModifyAsync(p =>
        {
            p.Components = new ComponentBuilder().WithButton(button).Build();
        });*/
    }

    private async Task StopMergePressed(SocketMessageComponent arg)
    {
        if (!arg.GuildId.HasValue)
            return;

        var allowedRoles = _config.Guilds[arg.GuildId.Value].MaintainerRoles;
        if (!CheckGuildRoles(arg.GuildId, arg.User.Id, allowedRoles))
            return;
        
        var modal = new ModalBuilder()
            .WithCustomId(StopMergeModal)
            .WithTitle("Stop automatic merge")
            .AddTextInput("Reason", ReasonInput, TextInputStyle.Paragraph)
            .Build();
                
        await arg.RespondWithModalAsync(modal);
    }

    private bool CheckGuildRoles(ulong? guildId, ulong userId, List<ulong> roles)
    {
        if (!guildId.HasValue)
            return false;

        var guild = _clientService.Client.GetGuild(guildId.Value);
        var user = guild.GetUser(userId);
        return user != null && user.Roles.Any(role => roles.Contains(role.Id));
    }
}