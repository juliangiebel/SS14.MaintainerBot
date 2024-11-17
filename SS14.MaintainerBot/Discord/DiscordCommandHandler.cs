using Discord;
using FastEndpoints;
using JetBrains.Annotations;
using SS14.MaintainerBot.Core.Configuration;
using SS14.MaintainerBot.Discord.Commands;
using SS14.MaintainerBot.Discord.Configuration;
using SS14.MaintainerBot.Discord.Entities;
using SS14.MaintainerBot.Discord.Types;
using SS14.MaintainerBot.Github;

namespace SS14.MaintainerBot.Discord;

[UsedImplicitly]
public sealed class DiscordCommandHandler :
    ICommandHandler<CreateReviewThreadPost, DiscordMessage?>,
    ICommandHandler<CreateOrUpdateForumPost, DiscordMessage?>,
    ICommandHandler<UpdateMergeProcessPost>,
    ICommandHandler<UpdateReviewThreadPostTags, DiscordMessage?>,
    ICommandHandler<CreateReviewThreadMessage>
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

    
    public async Task<DiscordMessage?> ExecuteAsync(CreateReviewThreadPost command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();
        
        var pullRequest = await _githubApiService.GetPullRequest(command.Installation, command.PullRequestNumber);
        if (pullRequest == null)
            return null;

        var model = new ReviewThreadTemplateModel(pullRequest, command.ReviewThread);
        var template = await _templateService.RenderTemplate("merge_process_post", model, _serverConfig.Language);

        var labels = pullRequest.Labels.Select(l => l.Name);
        var tags = _config.Guilds[command.GuildId].GetLabelTags(labels);
        //https://opengraph.githubassets.com/<any_hash_number>/<owner>/<repo>/pull/<pr_number>
        return await CreateForumPost(
            command.GuildId,
            $"{pullRequest.Number} - {pullRequest.Title}",
            template,
            command.ReviewThread.Id,
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

        var model = new ReviewThreadTemplateModel(pullRequest, command.ReviewThread);
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

    public async Task<DiscordMessage?> ExecuteAsync(UpdateReviewThreadPostTags command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();

        var message = await dbRepository.GetMessageFromProcess(command.GuildId, command.ReviewThreadId, ct);
        if (message == null)
            return null;
        
        var config = _config.Guilds[command.GuildId];
        var tags = config.GetLabelTags(command.GithubLabels);

        if (config.StatusTags.TryGetValue(command.PullRequestStatus, out var statusTag))
            tags.Add(statusTag);
        
        if (config.ProcessTags.TryGetValue(command.ProcessStatus, out var processTag))
            tags.Add(processTag);
        
        await _discordClientService.UpdateForumPostTags(message.GuildId, message.ChannelId, tags);

        var titleTags = config.GetTitleTags(tags);
        await _discordClientService.UpdateForumPostTitleTag(message.GuildId, message.ChannelId, titleTags);

        if (!config.ArchivalTags.Intersect(tags).Any()) return message;
        
        await _discordClientService.ArchiveForumThread(message.GuildId, message.ChannelId);
        await dbRepository.DeleteMessage(message, ct);
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
            ReviewThreadId = mergeProcessId,
            GuildId = channel.GuildId,
            ChannelId = channel.Id,
            MessageId = messageId
        };

        dbRepository.DbContext.Add(message);
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

    public async Task ExecuteAsync(CreateReviewThreadMessage command, CancellationToken ct)
    {
        var scope = _scopeFactory.CreateScope();
        var dbRepository = scope.Resolve<DiscordDbRepository>();

        foreach (var (id, guildConfig) in _config.Guilds)
        {
            if (!guildConfig.CheckInstallation(command.Installation))
                continue;

            var message = await dbRepository.GetMessageFromProcess(id, command.ReviewThread.Id, ct);
            if (message == null)
                return;
            
            var thread = await _discordClientService.GetThread(message.GuildId, message.ChannelId, message.MessageId);
            if (!thread.HasValue)
                return;

            var (channel, _) = thread.Value;
            await channel.SendMessageAsync(command.Message);
        }
    }
}