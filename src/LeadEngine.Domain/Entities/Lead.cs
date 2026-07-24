using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public Guid? CampanhaId { get; set; }
    public TipoLead Tipo { get; set; }
    public TipoContratacaoLead? TipoContratacao { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string WhatsAppNormalizado { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? EmailNormalizado { get; set; }
    public string? Cep { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public int? QuantidadeVidas { get; set; }
    public string? Observacao { get; set; }
    public string? IdadesJson { get; set; }
    public string? HospitalDesejado { get; set; }
    public string? OperadoraDesejada { get; set; }
    public bool? PossuiPlanoAtual { get; set; }
    public string? PlanoAtual { get; set; }
    public string? NomeEmpresa { get; set; }
    public string? Cnpj { get; set; }
    public string? CnpjNormalizado { get; set; }
    public int? QuantidadeFuncionarios { get; set; }
    public StatusLead Status { get; set; }
    public bool ConsentimentoContato { get; set; }
    public DateTime ConsentimentoEm { get; set; }
    public string TextoConsentimentoVersao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public string? OrigemCaptura { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgentResumo { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
    public string? Gclid { get; set; }
    public string? Fbclid { get; set; }
    public TipoAtribuicaoLead TipoAtribuicao { get; set; } = TipoAtribuicaoLead.NaoAtribuida;
    public Guid? GoogleAdsPublicacaoId { get; set; }
    public DateTime? DataAtribuicao { get; set; }
    public string? StatusEnvioExterno { get; set; }
    public int TentativasEnvioExterno { get; set; }
    public string? UltimoErroEnvioExterno { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public string? ErroEnvio { get; set; }
    public int? UltimoStatusHttpIntegracao { get; set; }
    public OrigemLead Origem { get; set; } = null!;
    public ICollection<LogIntegracaoLead> LogsIntegracao { get; set; } = new List<LogIntegracaoLead>();
    public Campanha? Campanha { get; set; }
}
