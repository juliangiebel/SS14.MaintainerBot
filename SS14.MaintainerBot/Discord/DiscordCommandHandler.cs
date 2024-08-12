using Discord;
using Discord.Rest;
using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Entities;

namespace SS14.MaintainerBot.Discord;

[UsedImplicitly]
public sealed class DiscordCommandHandler :
    ICommandHandler<CreateOrUpdateForumPost, DiscordMessage?>
{
    private readonly DiscordClientService _discordClientService;
    private readonly IServiceScopeFactory _scopeFactory;

    public DiscordCommandHandler(DiscordClientService discordClientService, IServiceScopeFactory scopeFactory)
    {
        _discordClientService = discordClientService;
        _scopeFactory = scopeFactory;
    }

    public async Task<DiscordMessage?> ExecuteAsync(CreateOrUpdateForumPost command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        var component = BuildButtons(command.Buttons);
        
        var post = await _discordClientService.CreateForumThread(command.GuildId, command.Title, command.Content, component);
        
        if (!post.HasValue)
            return null;

        var (channel, messageId) = post.Value;
        
        var message = new DiscordMessage
        {
            MergeProcessId = command.MergeProcessId,
            GuildId = channel.GuildId,
            ChannelId = channel.Id,
            MessageId = messageId
        };

        await dbRepository.DbContext.AddAsync(message, ct);
        return message;
    }

    private MessageComponent? BuildButtons(List<ButtonDefinition>? definitions)
    {
        if (definitions == null)
            return null;

        var builder = new ComponentBuilder();
        
        foreach (var definition in definitions)
        {
            var button = new ButtonBuilder()
                .WithLabel(definition.Title)
                .WithCustomId(definition.Id)
                .WithStyle(definition.Style)
                .WithDisabled(definition.Disabled);
            builder.WithButton(button);
        }

        return builder.Build();
    }
}