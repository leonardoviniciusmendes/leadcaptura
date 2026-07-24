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
    public string? BeneficiosJson { get; set; } = "[]";
    public string? PerguntasFrequentesJson { get; set; } = "[]";
    public string? PalavrasChaveJson { get; set; } = "[]";
    public string? PalavrasChaveNegativasJson { get; set; } = "[]";
    public string? TitulosAnunciosJson { get; set; } = "[]";
    public string? DescricoesAnunciosJson { get; set; } = "[]";
    public string? ErroGeracao { get; set; }
    public string? ProviderIa { get; set; }
    public string? ModeloIa { get; set; }
    public DateTime? DataGeracao { get; set; }
    public long? DuracaoGeracaoMs { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public bool Publicada { get; set; }
    public bool Ativo { get; set; }
    public DateTime? DataPublicacao { get; set; }
    public DateTime? DataDespublicacao { get; set; }
    public string? UrlPublica { get; set; }
    public ICollection<CampanhaRevisao> Revisoes { get; set; } = [];
    public ICollection<Lead> Leads { get; set; } = [];
}
