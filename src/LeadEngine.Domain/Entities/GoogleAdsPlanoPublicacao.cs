using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsPlanoPublicacao
{
    public Guid Id { get; set; }
    public Guid CampanhaId { get; set; }
    public Guid GoogleAdsContaId { get; set; }
    public string NomeCampanha { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public StatusPlanoPublicacaoGoogleAds Status { get; set; }
    public string TipoRede { get; set; } = "SEARCH";
    public decimal OrcamentoDiario { get; set; }
    public string CodigoMoeda { get; set; } = "BRL";
    public string Idioma { get; set; } = "pt";
    public string Pais { get; set; } = "BR";
    public string UrlFinal { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? DataValidacao { get; set; }
    public string ErrosValidacaoJson { get; set; } = "[]";
    public string AvisosValidacaoJson { get; set; } = "[]";
    public string PayloadPreviewJson { get; set; } = "{}";
    public int Versao { get; set; } = 1;
    public string ConteudoHash { get; set; } = string.Empty;

    public Campanha? Campanha { get; set; }
    public GoogleAdsConta? GoogleAdsConta { get; set; }
}
