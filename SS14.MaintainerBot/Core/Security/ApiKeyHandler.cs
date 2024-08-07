﻿using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SS14.MaintainerBot.Core.Security;

public class ApiKeyHandler : AuthenticationHandler<ApiKeyOptions>
{
    private const string InvalidApiKeyMessage = "Invalid API key";

    public const string HeaderName = "X-API-Key";

    public const string Name = "API_KEY";

    public ApiKeyHandler(
        IOptionsMonitor<ApiKeyOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = Options.ApiKey;

        if(key == null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if(!Request.Headers.TryGetValue(HeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.Fail(InvalidApiKeyMessage));

        if (!key.Equals(providedKey))
            return Task.FromResult(AuthenticateResult.Fail(InvalidApiKeyMessage));

        var claims = new[] { new Claim(ClaimTypes.Name, "API") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Name));
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}