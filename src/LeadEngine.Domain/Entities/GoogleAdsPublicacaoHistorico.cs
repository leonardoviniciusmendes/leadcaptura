using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsPublicacaoHistorico
{
    public Guid Id { get; set; }
    public Guid GoogleAdsPublicacaoId { get; set; }
    public StatusPublicacaoGoogleAds? StatusAnterior { get; set; }
    public StatusPublicacaoGoogleAds StatusNovo { get; set; }
    public string Operacao { get; set; } = string.Empty;
    public string? MensagemControlada { get; set; }
    public string? RequestId { get; set; }
    public DateTime Data { get; set; }
    public string MetadadosJson { get; set; } = "{}";

    public GoogleAdsPublicacao? Publicacao { get; set; }
}
