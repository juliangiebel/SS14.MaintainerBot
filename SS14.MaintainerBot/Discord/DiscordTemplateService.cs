using System.Globalization;
using Fluid;
using Fluid.Values;
using Microsoft.OpenApi.Extensions;
using Serilog;
using SS14.MaintainerBot.Discord.Configuration;
using ILogger = Serilog.ILogger;

namespace SS14.MaintainerBot.Discord;

public sealed class DiscordTemplateService
{
    private const string TemplateFileSearchPattern = "*.liquid";

    private readonly FluidParser _parser;
    private readonly DiscordConfiguration _configuration = new();
    private readonly Dictionary<string, IFluidTemplate> _templates = new();
    private readonly ILogger _log;

    public DiscordTemplateService(IConfiguration configuration, FluidParser parser)
    {
        _parser = parser;
        _log =  Log.ForContext<DiscordTemplateService>();;
        configuration.Bind(DiscordConfiguration.Name, _configuration);
    }
    
    public async Task LoadTemplates()
    {
        var path = _configuration.TemplateLocation;

        if (path == null)
        {
            _log.Error("Tried to load templates without template path configured [Discord.TemplateLocation]");
            return;
        }

        if (!Directory.Exists(path))
        {
            _log.Error("Template path doesn't exist: {TemplatePath}", path);
            return;
        }

        _log.Information("Preloading discord message templates");

        var directory = new DirectoryInfo(path);

        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 5
        };

        foreach (var templateFile in directory.EnumerateFiles(TemplateFileSearchPattern, enumerationOptions))
        {
            var rawTemplate = await File.ReadAllTextAsync(templateFile.FullName);
            if (!_parser.TryParse(rawTemplate, out var template, out var error))
            {
                _log.Error(
                    "Failed to parse template: {TemplateName}.\n{ErrorMessage}",
                    templateFile.Name,
                    error
                );
                continue;
            }

            var templateName = Path.GetRelativePath(path, templateFile.FullName);
            templateName = templateName.Replace(Path.GetExtension(templateName), "");
            _templates.Add(templateName, template);
        }

        _log.Information("Loaded {TemplateCount} templates", _templates.Count);
    }
    
    public async Task<string> RenderTemplate(string templateName, object? model = null, CultureInfo? culture = null)
    {
        model ??= new { };
        culture ??= CultureInfo.InvariantCulture;

        if (!_templates.TryGetValue(templateName, out var template))
        {
            _log.Error("No template with name: {TemplateName}", templateName);
            return "";
        }

        var context = new TemplateContext(model)
        {
            CultureInfo = culture,
            Options =
            {
                MemberAccessStrategy = new UnsafeMemberAccessStrategy()
            }
        };

        context.Options.Filters.AddFilter("discord_timestamp", DiscordDateFiler);
        context.Options.ValueConverters.Add(EnumConverter);
        context.SetValue("current_datetime", DateTime.Now);
        return await template.RenderAsync(context);
    }

    private object? EnumConverter(object arg)
    {
        return arg is not Enum value ? null : value.GetDisplayName();
    }

    /// <summary>
    /// Turns a date time into a discord timestamp string
    /// </summary>
    /// <remarks>
    /// For supported display styles see: <a href="https://discord.com/developers/docs/reference#message-formatting-timestamp-styles">Discord developer docs</a>
    /// </remarks>
    private ValueTask<FluidValue> DiscordDateFiler(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (!input.TryGetDateTimeInput(context, out var dateTime))
            return NilValue.Instance;

        var displayType =  arguments.At(0).ToStringValue();
        var timestamp = dateTime.ToUnixTimeSeconds();
        return StringValue.Create(string.IsNullOrWhiteSpace(displayType) ? $"<t:{timestamp}>" : $"<t:{timestamp}:{displayType}>");
    }
}