using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class Campanha
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoPublicoCampanha TipoPublico { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Regiao { get; set; }
    public string Operadora { get; set; } = string.Empty;
    public decimal OrcamentoDiario { get; set; }
    public string? Objetivo { get; set; }
    public StatusCampanha Status { get; set; }
    public string TituloLandingPage { get; set; } = string.Empty;
    public string SubtituloLandingPage { get; set; } = string.Empty;
    public string TextoBotao { get; set; } = string.Empty;
    public string MensagemWhatsApp { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}
