using FastEndpoints;
using SS14.MaintainerBot.Discord.Commands;

namespace SS14.MaintainerBot.Discord;

public sealed class DiscordCommandHandler :
    ICommandHandler<CreateForumPost, Guid?>
{
    private readonly DiscordClientService _discordClientService;

    public DiscordCommandHandler(DiscordClientService discordClientService)
    {
        _discordClientService = discordClientService;
    }

    public async Task<Guid?> ExecuteAsync(CreateForumPost command, CancellationToken ct)
    {
        await _discordClientService.CreateForumThread(command.GuildId, command.Title);
        return null;
    }
}