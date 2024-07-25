namespace SS14.MaintainerBot.Discord;

public sealed class DiscordConfiguration
{
    public const string Name = "Discord";
    
    public string Token { get; set; }
    public ulong GuildId { get; set; }
    public ulong ForumChannelId { get; set; }
}