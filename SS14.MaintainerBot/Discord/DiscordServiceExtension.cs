﻿using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Serilog;

namespace SS14.MaintainerBot.Discord;

public static class DiscordServiceExtension
{
    public static void AddDiscordClient(this IServiceCollection collection)
    {
        var config = new DiscordSocketConfig
        {
            // TODO: Set correct gateway intents
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions,
            LogLevel = LogSeverity.Verbose
            
        };

        var interactionConfig = new InteractionServiceConfig
        {
            LogLevel = LogSeverity.Verbose
        };
        
        collection.AddSingleton(config);
        collection.AddSingleton<DiscordSocketClient>();
        collection.AddSingleton<DiscordRestClient>(s => s.GetService<DiscordSocketClient>()!.Rest);
        collection.AddSingleton(interactionConfig);
        collection.AddSingleton<InteractionService>();
        collection.AddSingleton<DiscordClientService>();
    }

    public static async Task UseDiscordClient(this WebApplication app)
    {
        var client = app.Services.GetRequiredService<DiscordClientService>();

        var assembly = Assembly.GetExecutingAssembly();
        var interactionService = app.Services.GetRequiredService<InteractionService>();
        await interactionService.AddModulesAsync(assembly, app.Services);
        
        await client.Start();
    }
}