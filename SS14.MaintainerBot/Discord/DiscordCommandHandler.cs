using Discord;
using Discord.Rest;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.OpenApi.Extensions;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Discord.Entities;
using SS14.MaintainerBot.Discord.Types;
using SS14.MaintainerBot.Github;

namespace SS14.MaintainerBot.Discord;

[UsedImplicitly]
public sealed class DiscordCommandHandler :
    ICommandHandler<CreateMergeProcessPost, DiscordMessage?>,
    ICommandHandler<CreateOrUpdateForumPost, DiscordMessage?>,
    ICommandHandler<UpdateMergeProcessPost>,
    ICommandHandler<UpdateMergeProcessPostTags, DiscordMessage?>
{
    private readonly ServerConfiguration _serverConfig = new();
    private readonly DiscordConfiguration _config = new();
    
    private readonly DiscordClientService _discordClientService;
    private readonly IGithubApiService _githubApiService;
    private readonly DiscordTemplateService _templateService;
    private readonly IServiceScopeFactory _scopeFactory;

    public DiscordCommandHandler(
        DiscordClientService discordClientService, 
        IServiceScopeFactory scopeFactory, 
        IGithubApiService githubApiService, 
        DiscordTemplateService templateService,
        IConfiguration configuration)
    {
        _discordClientService = discordClientService;
        _scopeFactory = scopeFactory;
        _githubApiService = githubApiService;
        _templateService = templateService;
        configuration.Bind(ServerConfiguration.Name, _serverConfig);
        configuration.Bind(DiscordConfiguration.Name, _config);
    }

    
    public async Task<DiscordMessage?> ExecuteAsync(CreateMergeProcessPost command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        var pullRequest = await _githubApiService.GetPullRequest(command.Installation, command.PullRequestNumber);
        if (pullRequest == null)
            return null;

        var model = new ProcessPostTemplateModel(pullRequest, command.MergeProcess);
        var template = await _templateService.RenderTemplate("merge_process_post", model, _serverConfig.Language);

        var labels = pullRequest.Labels.Select(l => l.Name);
        var tags = _config.Guilds[command.GuildId].GetLabelTags(labels);
        
        return await CreateForumPost(
            command.GuildId,
            $"{pullRequest.Number} - {pullRequest.Title}",
            template,
            command.MergeProcess.Id,
            dbRepository, 
            command.Button,
            tags,
            ct);
    }
    
    public async Task ExecuteAsync(UpdateMergeProcessPost command, CancellationToken ct)
    {
        var pullRequest = await _githubApiService.GetPullRequest(command.Installation, command.PullRequestNumber);
        if (pullRequest == null)
            return;

        var model = new ProcessPostTemplateModel(pullRequest, command.MergeProcess);
        var template = await _templateService.RenderTemplate("merge_process_post", model, _serverConfig.Language);

        await UpdateForumPost(command.Message, template, command.Button);
    }
    
    public async Task<DiscordMessage?> ExecuteAsync(CreateOrUpdateForumPost command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        var component = BuildButtons(command.Buttons);
        
        return await CreateForumPost(
            command.GuildId,
            command.Title,
            command.Content,
            command.MergeProcessId,
            dbRepository, 
            component,
            command.Tags,
            ct);
    }

    public async Task<DiscordMessage?> ExecuteAsync(UpdateMergeProcessPostTags command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();

        var message = await dbRepository.GetMessageFromProcess(command.GuildId, command.MergeProcessId, ct);
        if (message == null)
            return null;
        
        var config = _config.Guilds[command.GuildId];
        var tags = config.GetLabelTags(command.GithubLabels);

        if (config.StatusTags.TryGetValue(command.PullRequestStatus, out var statusTag))
            tags.Add(statusTag);
        
        if (config.ProcessTags.TryGetValue(command.ProcessStatus, out var processTag))
            tags.Add(processTag);

        await _discordClientService.UpdateForumPostTags(message.GuildId, message.ChannelId, tags);
        return message;
    }
    
    private async Task<DiscordMessage?> CreateForumPost(
        ulong guildId,
        string title,
        string content,
        Guid mergeProcessId,
        DiscordDbRepository dbRepository, 
        MessageComponent? component,
        List<string>? tags,
        CancellationToken ct)
    {
        var post = await _discordClientService.CreateForumThread(guildId, title, content, component, tags);
        
        if (!post.HasValue)
            return null;

        var (channel, messageId) = post.Value;
        
        var message = new DiscordMessage
        {
            MergeProcessId = mergeProcessId,
            GuildId = channel.GuildId,
            ChannelId = channel.Id,
            MessageId = messageId
        };

        await dbRepository.DbContext.AddAsync(message, ct);
        await dbRepository.DbContext.SaveChangesAsync(ct);
        return message;
    }
    
    private async Task UpdateForumPost(DiscordMessage message, string content, MessageComponent? component)
    {
        var thread = await _discordClientService.GetThread(message.GuildId, message.ChannelId, message.MessageId);
        if (!thread.HasValue)
            return;

        var (channel, post) = thread.Value;

        await channel.ModifyMessageAsync(post.Id, p =>
        {
            p.Content = content;
            p.Components = Optional.Create(component);
        });
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