using Discord.Interactions;

namespace SS14.MaintainerBot.Discord.DiscordCommands;

public class ManagementModule : InteractionModuleBase
{

    [SlashCommand("status", "Shows the bots status")]
    public async Task StatusCommand()
    {
        await RespondAsync("Status");
    }
}