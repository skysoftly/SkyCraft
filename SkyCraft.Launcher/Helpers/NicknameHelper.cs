using System.Text.RegularExpressions;

namespace SkyCraft.Launcher.Helpers;

public static partial class NicknameHelper
{
    [GeneratedRegex("^[A-Za-z0-9_]{3,16}$")]
    private static partial Regex NicknameRegex();

    public static bool IsValid(string nickname)
    {
        return !string.IsNullOrWhiteSpace(nickname) &&
               NicknameRegex().IsMatch(nickname);
    }
}