using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using SS14.GithubApiHelper.Extensions;
using SS14.GithubApiHelper.Services;
using SS14.MaintainerBot.Configuration;
using SS14.MaintainerBot.Discord;
using SS14.MaintainerBot.Discord.DiscordCommands;
using SS14.MaintainerBot.Github;
using SS14.MaintainerBot.Github.Services;
using SS14.MaintainerBot.Helpers;
using SS14.MaintainerBot.Models;
using SS14.MaintainerBot.Scheduler;

var builder = WebApplication.CreateBuilder(args);

#region Configuration

// Configuration as yaml
builder.Configuration.AddYamlFile("appsettings.yaml", false, true);
builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", true, true);
builder.Configuration.AddYamlFile("appsettings.Secret.yaml", true, true);

var serverConfiguration = new ServerConfiguration();
builder.Configuration.Bind(ServerConfiguration.Name, serverConfiguration);

#endregion

#region Server

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(serverConfiguration.CorsOrigins.ToArray());
        policy.AllowCredentials();
    });
});

//Forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
});

//Logging
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
builder.Logging.AddSerilog();

//Systemd Support
builder.Host.UseSystemd();

//Sentry
if (serverConfiguration.EnableSentry)
    builder.WebHost.UseSentry();

#endregion

#region Database

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("default"));
await using var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<Context>(opt =>
{
    // ReSharper disable once AccessToDisposedClosure
    opt.UseNpgsql(dataSource);
});

#endregion

#region AddServices

// Services
builder.Services.AddFastEndpoints().SwaggerDocument();
builder.Services.AddSingleton<RateLimiterService>();

// Github
//builder.Services.Configure<GithubBotConfiguration>(builder.Configuration.GetSection(GithubBotConfiguration.Name));
builder.Services.AddScoped<GithubDbRepository>();
builder.Services.AddSingleton<GithubApiService>();
builder.Services.AddSingleton<PrVerificationService>();
builder.Services.AddGithubTemplating();

// Discord
//builder.Services.AddDiscordClient();
//builder.Services.AddSingleton<ManagementModule>();

// Scheduler
builder.Services.AddScheduler();

#endregion

var app = builder.Build();

//Migrate on startup
new StartupMigrationHelper().Migrate<Context>(app);

// Configure the HTTP request pipeline.
if (serverConfiguration.PathBase != null)
{
    app.UsePathBase(serverConfiguration.PathBase);
}

if (serverConfiguration.UseForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if ((app.Environment.IsProduction() || app.Environment.IsStaging()) && serverConfiguration.UseHttps)
{
    app.UseHttpsRedirection();
    //If this gets disabled by Server->UseHttps then Hsts is usually set up by a reverse proxy
    app.UseHsts();
}

app.UseCors();

await app.PreloadGithubTemplates();

if (serverConfiguration is { EnableSentry: true, EnableSentryTracing: true })
    app.UseSentryTracing();

var endpoints = app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    endpoints.UseSwaggerGen();
}

app.ScheduleMarkedJobs();
app.Run();