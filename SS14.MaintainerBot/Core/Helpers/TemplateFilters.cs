using System.Text.RegularExpressions;
using Fluid;
using Fluid.Values;

namespace SS14.MaintainerBot.Core.Helpers;

public static partial class TemplateFilters
{
    [GeneratedRegex(@"<!--(?:.|\n|\r|\t)*?-->")]
    private static partial Regex HtmlCommentRegex();

    public static ValueTask<FluidValue> StripHtmlComments(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type != FluidValues.String)
            return NilValue.Instance;

        var value = input.ToStringValue();
        if (value == null)
            return NilValue.Instance;

        var strippedValue = HtmlCommentRegex().Replace(value, string.Empty);
        return StringValue.Create(strippedValue);
    }

    public static ValueTask<FluidValue> TruncateLines(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type != FluidValues.String || !arguments.At(0).IsInteger())
            return NilValue.Instance;

        var value = input.ToStringValue();
        if (value == null)
            return NilValue.Instance;

        var maxLines = decimal.ToInt32(arguments.At(0).ToNumberValue());
        var lastNewline = -1;
        var line = 0;
        
        while(line <= maxLines)
        {
            lastNewline = value.IndexOf('\n', lastNewline + 1);
            line++;
        }
        
        return lastNewline == -1 ? input : StringValue.Create($"{value[..(lastNewline - 1)]}\n-# :scissors: Text truncated...");
    }
}