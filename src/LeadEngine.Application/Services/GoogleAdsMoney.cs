namespace LeadEngine.Application.Services;

public static class GoogleAdsMoney
{
    public static long ToMicros(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Valor monetario nao pode ser negativo.");
        }

        return decimal.ToInt64(decimal.Round(amount * 1_000_000m, 0, MidpointRounding.AwayFromZero));
    }
}
