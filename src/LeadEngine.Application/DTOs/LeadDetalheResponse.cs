using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record LeadDetalheResponse
{
    public Guid Id { get; init; }
    public TipoLead Tipo { get; init; }
    public StatusLead Status { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string WhatsAppMascarado { get; init; } = string.Empty;
    public string? EmailMascarado { get; init; }
    public string? CepMascarado { get; init; }
    public string? Cidade { get; init; }
    public string? Uf { get; init; }
    public int? QuantidadeVidas { get; init; }
    public string? IdadesJson { get; init; }
    public string? HospitalDesejado { get; init; }
    public string? OperadoraDesejada { get; init; }
    public bool? PossuiPlanoAtual { get; init; }
    public string? PlanoAtual { get; init; }
    public string? NomeEmpresa { get; init; }
    public string? CnpjMascarado { get; init; }
    public int? QuantidadeFuncionarios { get; init; }
    public bool ConsentimentoContato { get; init; }
    public DateTime ConsentimentoEm { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? EnviadoEm { get; init; }
    public string? ErroEnvio { get; init; }
    public OrigemLeadDto? Origem { get; init; }
    public IReadOnlyCollection<LogIntegracaoLeadDto> LogsIntegracao { get; init; } = Array.Empty<LogIntegracaoLeadDto>();
}
