﻿using FastEndpoints;
using SS14.MaintainerBot.Github.Entities;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Commands;

public record GetPullRequest(InstallationIdentifier Installation, int Number) : ICommand<PullRequest?>;
