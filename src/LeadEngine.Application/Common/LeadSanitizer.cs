using System.Text.RegularExpressions;

namespace LeadEngine.Application.Common;

public static partial class LeadSanitizer
{
    public static string? Texto(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = DangerousCharsRegex().Replace(value.Trim(), string.Empty);
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }

    public static string Digitos(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    public static string? Email(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }

    public static string MascaraTelefone(string value)
    {
        var digits = Digitos(value);
        if (digits.Length <= 4)
        {
            return "****";
        }

        return $"{digits[..2]}*****{digits[^4..]}";
    }

    public static string? MascaraEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
        {
            return value;
        }

        var parts = value.Split('@', 2);
        var prefix = parts[0].Length <= 2 ? "***" : $"{parts[0][..2]}***";
        return $"{prefix}@{parts[1]}";
    }

    public static string? MascaraDocumento(string? value)
    {
        var digits = Digitos(value);
        return digits.Length < 4 ? null : $"***{digits[^4..]}";
    }

    [GeneratedRegex("[<>\"'`;]")]
    private static partial Regex DangerousCharsRegex();
}
