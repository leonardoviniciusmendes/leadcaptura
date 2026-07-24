using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsSincronizacao
{
    public Guid Id { get; set; }
    public Guid? GoogleAdsPublicacaoId { get; set; }
    public Guid GoogleAdsContaId { get; set; }
    public TipoSincronizacaoGoogleAds Tipo { get; set; }
    public StatusSincronizacaoGoogleAds Status { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataConclusao { get; set; }
    public DateOnly? PeriodoInicial { get; set; }
    public DateOnly? PeriodoFinal { get; set; }
    public int RegistrosConsultados { get; set; }
    public int RegistrosCriados { get; set; }
    public int RegistrosAtualizados { get; set; }
    public string? RequestId { get; set; }
    public string? ErroCodigo { get; set; }
    public string? ErroMensagemControlada { get; set; }
    public long DuracaoMs { get; set; }
    public DateTime DataCriacao { get; set; }

    public GoogleAdsPublicacao? Publicacao { get; set; }
    public GoogleAdsConta? GoogleAdsConta { get; set; }
}
