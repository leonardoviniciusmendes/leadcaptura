namespace LeadEngine.Application.Services;

public static class MetaAdsMoney
{
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    public static long ToMinorUnits(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Valor monetario nao pode ser negativo.");
        }

        var multiplier = ZeroDecimalCurrencies.Contains(currency) ? 1m : 100m;
        return decimal.ToInt64(decimal.Round(amount * multiplier, 0, MidpointRounding.AwayFromZero));
    }
}
