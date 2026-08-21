namespace LeadEngine.Domain.Entities;

public sealed class MetaAdsImagem
{
    public Guid Id { get; set; }
    public Guid CampanhaId { get; set; }
    public Guid MetaAdsContaId { get; set; }
    public string AdAccountId { get; set; } = string.Empty;
    public string OrigemImagem { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long? TamanhoBytes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string MetaImageHash { get; set; } = string.Empty;
    public DateTime DataUpload { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public Campanha Campanha { get; set; } = null!;
    public MetaAdsConta MetaAdsConta { get; set; } = null!;
}
