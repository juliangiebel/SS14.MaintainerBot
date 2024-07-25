using Discord;
using Discord.WebSocket;

namespace SS14.MaintainerBot.Discord;

public sealed class DiscordClientService
{
    private readonly DiscordConfiguration _configuration = new();
    private readonly DiscordSocketClient _client;
    private readonly ILogger _logger;

    public DiscordClientService(IConfiguration configuration, ILogger logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
        configuration.Bind(DiscordConfiguration.Name, _configuration);

        _client.Log += Log;
    }
    
    // TODO: check if I have to call this
    public async Task Start()
    {
        await _client.LoginAsync(TokenType.Bot, _configuration.Token);
        await _client.StartAsync();
    }

    // TODO: Finish implementing this method
    public async Task CreateForumThread(string title)
    {
        var guild = _client.GetGuild(_configuration.GuildId);
        var channel = guild.GetForumChannel(_configuration.ForumChannelId);

        var post = await channel.CreatePostAsync(
            title,
            text: "wawa"
        );
    }
    
    private async Task Log(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };
        
        _logger.Log(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }
}