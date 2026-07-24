namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsAnaliseIa
{
    public Guid Id { get; set; }
    public Guid GoogleAdsPublicacaoId { get; set; }
    public DateOnly PeriodoInicial { get; set; }
    public DateOnly PeriodoFinal { get; set; }
    public string? Modelo { get; set; }
    public string? Provider { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public string ResultadoJson { get; set; } = "{}";
    public int? TokensEntrada { get; set; }
    public int? TokensSaida { get; set; }
    public decimal? CustoEstimado { get; set; }
    public long DuracaoMs { get; set; }
    public DateTime DataCriacao { get; set; }
    public bool Aplicada { get; set; }
    public DateTime? DataAplicacao { get; set; }

    public GoogleAdsPublicacao? Publicacao { get; set; }
}
