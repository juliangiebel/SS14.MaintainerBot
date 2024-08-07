using Microsoft.AspNetCore.Authentication;

namespace SS14.MaintainerBot.Core.Security;

public class ApiKeyOptions  : AuthenticationSchemeOptions
{
    public string? ApiKey { get; set; }
}