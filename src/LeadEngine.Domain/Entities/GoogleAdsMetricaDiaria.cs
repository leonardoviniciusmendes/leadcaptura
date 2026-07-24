namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsMetricaDiaria
{
    public Guid Id { get; set; }
    public Guid GoogleAdsPublicacaoId { get; set; }
    public Guid GoogleAdsContaId { get; set; }
    public string CampaignResourceName { get; set; } = string.Empty;
    public string CampaignExternalId { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public long Impressoes { get; set; }
    public long Cliques { get; set; }
    public long CustoMicros { get; set; }
    public decimal Custo { get; set; }
    public decimal Ctr { get; set; }
    public long CpcMedioMicros { get; set; }
    public decimal CpcMedio { get; set; }
    public decimal Conversoes { get; set; }
    public decimal ValorConversoes { get; set; }
    public decimal TaxaConversao { get; set; }
    public decimal? ParcelaImpressoesPesquisa { get; set; }
    public decimal? TaxaTopoPagina { get; set; }
    public decimal? TaxaTopoAbsoluto { get; set; }
    public DateTime DataSincronizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public GoogleAdsPublicacao? Publicacao { get; set; }
    public GoogleAdsConta? GoogleAdsConta { get; set; }
}
