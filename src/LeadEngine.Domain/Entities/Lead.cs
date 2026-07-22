using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public TipoLead Tipo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string WhatsAppNormalizado { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? EmailNormalizado { get; set; }
    public string? Cep { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public int? QuantidadeVidas { get; set; }
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
    public DateTime? EnviadoEm { get; set; }
    public string? ErroEnvio { get; set; }
    public int? UltimoStatusHttpIntegracao { get; set; }
    public OrigemLead Origem { get; set; } = null!;
    public ICollection<LogIntegracaoLead> LogsIntegracao { get; set; } = new List<LogIntegracaoLead>();
}
