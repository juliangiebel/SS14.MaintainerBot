using Discord.Interactions;

namespace SS14.MaintainerBot.Discord.DiscordCommands;

public class ManagementModule : InteractionModuleBase<SocketInteractionContext>
{

    [SlashCommand("status", "Shows the bots status")]
    public async Task StatusCommand()
    {
        // This is for testing slash commands
        await DeferAsync();
        await Task.Delay(TimeSpan.FromSeconds(5));
        await ModifyOriginalResponseAsync(p => p.Content = "status");
    }
}