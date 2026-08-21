namespace LeadEngine.Domain.Entities;

public sealed class MetaAdsAtivoSelecionado
{
    public Guid Id { get; set; }
    public Guid MetaAdsContaId { get; set; }
    public string? BusinessId { get; set; }
    public string? BusinessNome { get; set; }
    public string? AdAccountId { get; set; }
    public string? AdAccountNome { get; set; }
    public string? PageId { get; set; }
    public string? PageNome { get; set; }
    public string? InstagramAccountId { get; set; }
    public string? InstagramNome { get; set; }
    public string? PixelId { get; set; }
    public string? PixelNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public MetaAdsConta MetaAdsConta { get; set; } = null!;
}
