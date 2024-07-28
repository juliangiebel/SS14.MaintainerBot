using Octokit;

namespace SS14.MaintainerBot.Github.Helpers;

/// <summary>
/// Octokit bugs go brrr (╯°□°)╯︵ ┻━┻
/// </summary>
public static class OctokitFuckupsExtension
{
    /// <summary>
    /// Parses string value ignoring case
    /// <remarks>
    /// Stalebot goes brrr <br/>
    /// https://github.com/octokit/octokit.net/issues/2337
    /// </remarks>
    /// </summary>
    public static T Val<T>(this StringEnum<T> stringEnum) where T : struct
    {
        return Enum.Parse<T>(stringEnum.StringValue.Replace("_", ""), true);
    }
}