using System.Text.RegularExpressions;

namespace Sayiad.Domain.Common;

public static class InputSanitizer
{
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return Regex.Replace(input, @"<[^>]*>", string.Empty).Trim();
    }

    public static string? SanitizeNullable(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var cleaned = Regex.Replace(input, @"<[^>]*>", string.Empty).Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}
