namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsOperacaoPublicacao
{
    public Guid Id { get; set; }
    public Guid GoogleAdsPublicacaoId { get; set; }
    public int Indice { get; set; }
    public string TipoOperacao { get; set; } = string.Empty;
    public string? EntidadeOrigem { get; set; }
    public string? ResourceNameTemporario { get; set; }
    public string? ResourceNameDefinitivo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CodigoErro { get; set; }
    public string? MensagemControlada { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataConclusao { get; set; }

    public GoogleAdsPublicacao? Publicacao { get; set; }
}
