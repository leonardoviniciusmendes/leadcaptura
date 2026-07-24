using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsPublicacao
{
    public Guid Id { get; set; }
    public Guid GoogleAdsPlanoPublicacaoId { get; set; }
    public Guid CampanhaId { get; set; }
    public Guid GoogleAdsContaId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int PreviewVersao { get; set; }
    public string PreviewHash { get; set; } = string.Empty;
    public StatusPublicacaoGoogleAds Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ConfirmationTokenHash { get; set; }
    public DateTime? ConfirmationExpiresAt { get; set; }
    public DateTime? DataPreparacao { get; set; }
    public DateTime? DataValidacaoRemota { get; set; }
    public DateTime? DataInicioPublicacao { get; set; }
    public DateTime? DataConclusao { get; set; }
    public string? RequestIdValidacao { get; set; }
    public string? RequestIdPublicacao { get; set; }
    public string? ErroCodigo { get; set; }
    public string? ErroMensagemControlada { get; set; }
    public string ErrosJson { get; set; } = "[]";
    public string RecursosJson { get; set; } = "[]";
    public int Tentativas { get; set; }
    public bool Teste { get; set; }
    public string? GeoTargetResourceName { get; set; }
    public string? LanguageResourceName { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public GoogleAdsPlanoPublicacao? PlanoPublicacao { get; set; }
    public Campanha? Campanha { get; set; }
    public GoogleAdsConta? GoogleAdsConta { get; set; }
    public ICollection<GoogleAdsRecursoPublicado> Recursos { get; set; } = [];
}
