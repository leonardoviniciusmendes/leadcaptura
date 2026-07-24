namespace LeadEngine.Application.Services;

public static class GoogleAdsPeriod
{
    public static (DateOnly Start, DateOnly End) Resolve(DateOnly? start, DateOnly? end, int defaultDays = 30)
    {
        var final = end ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var initial = start ?? final.AddDays(-Math.Max(defaultDays - 1, 0));
        if (initial > final) throw new ArgumentException("Data inicial deve ser menor ou igual a data final.");
        if (final.DayNumber - initial.DayNumber > 89) throw new ArgumentException("Periodo maximo para sincronizacao Google Ads e 90 dias.");
        return (initial, final);
    }
}
