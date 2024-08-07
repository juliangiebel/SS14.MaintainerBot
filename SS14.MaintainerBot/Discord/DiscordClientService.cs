using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog.Events;
using SS14.MaintainerBot.Discord.Configuration;

namespace SS14.MaintainerBot.Discord;

public sealed class DiscordClientService
{
    private readonly DiscordConfiguration _configuration = new();
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly Serilog.ILogger _logger;

    public bool Enabled { get; private set; }

    public DiscordClientService(IConfiguration configuration, IServiceProvider services, DiscordSocketClient client, InteractionService interactionService)
    {
        _logger = Serilog.Log.ForContext<DiscordClientService>();
        _client = client;
        _services = services;
        _interactionService = interactionService;
        configuration.Bind(DiscordConfiguration.Name, _configuration);

        _client.Log += Log;
        _interactionService.Log += Log;

        _client.Ready += Ready;
        _client.InteractionCreated += Interaction;
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
        
        await _client.LoginAsync(TokenType.Bot, _configuration.Token);
        await _client.StartAsync();
        Enabled = true;
    }

    // TODO: Finish implementing this method
    public async Task CreateForumThread(ulong guildId, string title)
    {
        if (!Enabled)
            return;
        
        var guild = _client.GetGuild(guildId);
        var channel = guild.GetForumChannel(_configuration.Guilds[guildId].ForumChannelId);

        var post = await channel.CreatePostAsync(
            title,
            text: "wawa"
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
        var ctx = new SocketInteractionContext(_client, arg);
        var result = await _interactionService.ExecuteCommandAsync(ctx, _services);
    }
}