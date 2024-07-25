using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace SS14.MaintainerBot.Discord;

public static class DiscordServiceExtension
{
    public static void AddDiscordClient(this IServiceCollection collection)
    {
        var config = new DiscordSocketConfig
        {
            // TODO: Set correct gateway intents
            GatewayIntents = GatewayIntents.AllUnprivileged
        };

        var interactionConfig = new InteractionServiceConfig
        {
        };
        
        collection.AddSingleton(config);
        collection.AddSingleton<DiscordSocketClient>();
        //collection.AddSingleton<DiscordRestClient>(s => s.GetService<DiscordSocketClient>()!.Rest);
        //collection.AddSingleton(interactionConfig);
        //collection.AddSingleton<InteractionService>();
        collection.AddSingleton<DiscordClientService>();
    }
}