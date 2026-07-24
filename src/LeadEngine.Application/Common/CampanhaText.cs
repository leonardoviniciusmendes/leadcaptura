using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LeadEngine.Application.Common;

public static partial class CampanhaText
{
    public static string? Limitar(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        var cleaned = NonSlugChars().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-");
        return DuplicateDash().Replace(cleaned, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateDash();
}
