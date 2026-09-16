using System.Text.RegularExpressions;

namespace LeadEngine.Application.Common;

public static partial class LeadSanitizer
{
    public const string CelularBrasileiroInvalido = "Informe um WhatsApp valido com DDD. Ex.: (21) 99999-9999.";

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

    public static bool TryNormalizarCelularBrasileiro(string? value, out string normalizado)
    {
        normalizado = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is < '0' or > '9'
                && !char.IsWhiteSpace(character)
                && character is not '(' and not ')' and not '-' and not '+')
            {
                return false;
            }
        }

        var digits = Digitos(value);
        if (digits.Length == 13 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.Length != 11 || digits[2] != '9')
        {
            return false;
        }

        normalizado = digits;
        return true;
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
