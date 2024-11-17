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
    public const string StopMergeButtonId = "stop-merge";
    private const string StopMergeModalId = "stop-merge-modal";
    private const string ReasonInputId = "reason";
    
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
            case StopMergeModalId: await StopMergeSubmitted(dbRepository, arg); break;
        }
    }

    

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        //var scope = _scopeFactory.CreateScope();
        //var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        switch (arg.Data.CustomId)
        {
            case StopMergeButtonId: await StopMergePressed(arg); break;
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
        var command = new ChangeReviewThreadStatus(
            new InstallationIdentifier(config.GithubInstallationId, config.GithubRepositoryId),
            message.ReviewThread.PullRequest.Number,
            MaintainerReviewStatus.Rejected);

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
            .WithCustomId(StopMergeModalId)
            .WithTitle("Stop automatic merge")
            .AddTextInput("Reason", ReasonInputId, TextInputStyle.Paragraph)
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