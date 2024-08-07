namespace SS14.MaintainerBot.Discord.Configuration;

public sealed class DiscordConfiguration
{
    public const string Name = "Discord";

    public string? Token { get; set; }

    public Dictionary<ulong, GuildConfiguration> Guilds { get; set; } = new();

}