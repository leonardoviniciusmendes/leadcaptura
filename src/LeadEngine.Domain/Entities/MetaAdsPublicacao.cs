using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class MetaAdsPublicacao
{
    public Guid Id { get; set; }
    public Guid CampanhaId { get; set; }
    public Guid MetaAdsContaId { get; set; }
    public string AdAccountId { get; set; } = string.Empty;
    public StatusPublicacaoMetaAds Status { get; set; }
    public string UltimaEtapaConcluida { get; set; } = string.Empty;
    public string? CampaignExternalId { get; set; }
    public string? AdSetExternalId { get; set; }
    public string? CreativeExternalId { get; set; }
    public string? AdExternalId { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataConclusao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public string? UltimoErroCodigo { get; set; }
    public string? UltimoErroSubcodigo { get; set; }
    public string? UltimoErroTipo { get; set; }
    public string? UltimoErroMensagem { get; set; }
    public string? UltimoErroHttpStatus { get; set; }
    public string? FbTraceId { get; set; }
    public Campanha Campanha { get; set; } = null!;
    public MetaAdsConta MetaAdsConta { get; set; } = null!;
}
