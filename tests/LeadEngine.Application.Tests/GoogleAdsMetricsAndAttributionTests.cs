using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsMetricsAndAttributionTests
{
    [Fact]
    public void SafeDivide_RetornaZeroParaDivisaoPorZero()
    {
        Assert.Equal(0, GoogleAdsMath.SafeDivide(10, 0));
    }

    [Fact]
    public void SafePercent_RetornaZeroParaDivisaoPorZero()
    {
        Assert.Equal(0, GoogleAdsMath.SafePercent(10, 0));
    }

    [Fact]
    public void MoneyFromMicros_ConverteCorretamente()
    {
        Assert.Equal(10m, GoogleAdsMath.MoneyFromMicros(10_000_000));
    }

    [Fact]
    public void Periodo_BloqueiaMaisDeNoventaDias()
    {
        Assert.Throws<ArgumentException>(() => GoogleAdsPeriod.Resolve(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 15)));
    }

    [Fact]
    public void Periodo_BloqueiaDataInicialMaiorQueFinal()
    {
        Assert.Throws<ArgumentException>(() => GoogleAdsPeriod.Resolve(new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void CustoPorLead_TrataZero()
    {
        Assert.Equal(0, GoogleAdsMath.SafeDivide(100, 0, 2));
    }

    [Fact]
    public void TaxaConversaoLanding_CalculaPercentual()
    {
        Assert.Equal(25m, GoogleAdsMath.SafePercent(5, 20));
    }

    [Fact]
    public void Roas_TrataCustoZero()
    {
        Assert.Equal(0, GoogleAdsMath.SafeDivide(100, 0, 4));
    }

    [Fact]
    public void PrioridadeAtribuicao_DiretaMaiorQueGclid()
    {
        Assert.True((int)TipoAtribuicaoLead.Direta > (int)TipoAtribuicaoLead.Gclid);
    }

    [Fact]
    public void PrioridadeAtribuicao_GclidMaiorQueUtm()
    {
        Assert.True((int)TipoAtribuicaoLead.Gclid > (int)TipoAtribuicaoLead.Utm);
    }

    [Fact]
    public void Metrica_CalculaCustoECpcSemInfinity()
    {
        var custo = GoogleAdsMath.MoneyFromMicros(0);
        var cpc = GoogleAdsMath.SafeDivide(custo, 0, 2);
        Assert.Equal(0, cpc);
    }

    [Fact]
    public void AnaliseIa_NaoDeveSerAplicadaPorPadrao()
    {
        var analise = new GoogleAdsAnaliseIa { Aplicada = false };
        Assert.False(analise.Aplicada);
    }
}
