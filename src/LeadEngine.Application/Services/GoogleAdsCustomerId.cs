namespace LeadEngine.Application.Services;

public static class GoogleAdsCustomerId
{
    public static string Normalize(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 10)
        {
            throw new ArgumentException("CustomerId Google Ads deve conter 10 digitos.");
        }

        return digits;
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return normalized.Length == 10;
    }

    public static string DigitsOnly(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static string Mask(string? value)
    {
        var digits = DigitsOnly(value);
        return digits.Length < 4 ? "****" : $"{digits[..2]}****{digits[^2..]}";
    }

    public static string CustomerResourceName(string value) => $"customers/{Normalize(value)}";
}
