namespace LeadEngine.Domain.Entities;

public sealed class MetaAdsPreparacaoPublicacao
{
    public Guid Id { get; set; }
    public Guid CampanhaId { get; set; }
    public Guid MetaAdsContaId { get; set; }
    public string AdAccountId { get; set; } = string.Empty;
    public string? LocationKey { get; set; }
    public string? LocationName { get; set; }
    public string? LocationType { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public string? Region { get; set; }
    public string? RegionId { get; set; }
    public int AgeMin { get; set; } = 25;
    public int AgeMax { get; set; } = 65;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public Campanha Campanha { get; set; } = null!;
    public MetaAdsConta MetaAdsConta { get; set; } = null!;
}
