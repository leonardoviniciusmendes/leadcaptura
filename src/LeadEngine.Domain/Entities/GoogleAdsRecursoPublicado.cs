namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsRecursoPublicado
{
    public Guid Id { get; set; }
    public Guid GoogleAdsPublicacaoId { get; set; }
    public string TipoRecurso { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? Nome { get; set; }
    public string Status { get; set; } = "PAUSED";
    public DateTime DataCriacao { get; set; }

    public GoogleAdsPublicacao? Publicacao { get; set; }
}
