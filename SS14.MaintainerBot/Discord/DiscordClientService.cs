using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog.Events;
using SS14.MaintainerBot.Discord.Configuration;
using IResult = Discord.Interactions.IResult;

namespace SS14.MaintainerBot.Discord;

public sealed class DiscordClientService
{
    public DiscordSocketClient Client { get; }
    
    private readonly DiscordConfiguration _configuration = new();
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly Serilog.ILogger _logger;

    public bool Enabled { get; private set; }

    public DiscordClientService(IConfiguration configuration, IServiceProvider services, DiscordSocketClient client, InteractionService interactionService)
    {
        _logger = Serilog.Log.ForContext<DiscordClientService>();
        Client = client;
        _services = services;
        _interactionService = interactionService;
        configuration.Bind(DiscordConfiguration.Name, _configuration);

        Client.Log += Log;
        _interactionService.Log += Log;

        Client.Ready += Ready;
        Client.InteractionCreated += Interaction;

        _interactionService.InteractionExecuted += InteractionExecuted;
    }
    
    // TODO: check if I have to call this
    public async Task Start()
    {
        if (_configuration.Token == null)
        {
            _logger.Warning("No discord token configured. Disabling discord integration");
            Enabled = false;
            return;
        }
        
        await Client.LoginAsync(TokenType.Bot, _configuration.Token);
        await Client.StartAsync();
        Enabled = true;
    }

    // TODO: Finish implementing this method
    public async Task CreateForumThread(ulong guildId, string title)
    {
        if (!Enabled)
            return;
        
        var guild = Client.GetGuild(guildId);
        var channel = guild.GetForumChannel(_configuration.Guilds[guildId].ForumChannelId);

        var row = new ActionRowBuilder()
            .WithButton("Stop Merge", "stop-merge", ButtonStyle.Danger);
        var component = new ComponentBuilder().AddRow(row).Build();
        
        var post = await channel.CreatePostAsync(
            title,
            text: "wawa",
            components: component
            
        );
    }
    
    public async Task Log(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        
        _logger.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }
    
    private async Task Ready()
    {
        await _interactionService.RegisterCommandsGloballyAsync();
    }
    
    private async Task Interaction(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(Client, arg);
        if (!_configuration.Guilds.ContainsKey(ctx.Guild.Id))
        {
            await ctx.Interaction.RespondAsync(
                @"```ansi
[2;31m[0m[1;2m[0m[1;2m[1;31m⚠[0m Maintainer bot isn't configured for this server.[0m
```");
    
            return;
        }
        
        await _interactionService.ExecuteCommandAsync(ctx, _services);
    }
    
    private Task InteractionExecuted(ICommandInfo? commandInfo, IInteractionContext ctx, IResult result)
    {
        if (commandInfo == null || result.IsSuccess)
            return Task.CompletedTask;

        /*if (commandInfo == null)
        {
            _logger.Error("Error while handling interaction: {ErrorMessage}", result.ErrorReason);
            return Task.CompletedTask;
        }*/
        
        _logger.Error(
            "Error while executing interaction [{CommandName}]: {ErrorMessage}",
            commandInfo.Name,
            result.ErrorReason);

        ctx.Interaction.ModifyOriginalResponseAsync(p => p.Content = "Error while processing slash command.");
        return Task.CompletedTask;
    }
}