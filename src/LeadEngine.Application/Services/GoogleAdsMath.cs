namespace LeadEngine.Application.Services;

public static class GoogleAdsMath
{
    public static decimal MoneyFromMicros(long micros) => decimal.Round(micros / 1_000_000m, 2);
    public static decimal SafeDivide(decimal numerator, decimal denominator, int decimals = 4) => denominator == 0 ? 0 : decimal.Round(numerator / denominator, decimals);
    public static decimal SafePercent(decimal numerator, decimal denominator) => denominator == 0 ? 0 : decimal.Round(numerator / denominator * 100m, 4);
}
