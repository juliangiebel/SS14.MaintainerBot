﻿
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Discord.Configuration;

public sealed class GuildConfiguration
{
    public long GithubInstallationId { get; set; }
    public long GithubRepositoryId { get; set; }
    public ulong ForumChannelId { get; set; }
    public List<ulong> MaintainerRoles { get; set; } = [];

    public bool CheckInstallation(InstallationIdentifier installation)
    {
        return GithubInstallationId == installation.InstallationId && GithubRepositoryId == installation.RepositoryId;
    }
}