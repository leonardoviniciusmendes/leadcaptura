using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record LeadDetalheResponse
{
    public Guid Id { get; init; }
    public Guid? CampanhaId { get; init; }
    public string? CampanhaNome { get; init; }
    public TipoLead Tipo { get; init; }
    public TipoContratacaoLead? TipoContratacao { get; init; }
    public StatusLead Status { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string WhatsAppMascarado { get; init; } = string.Empty;
    public string? EmailMascarado { get; init; }
    public string? CepMascarado { get; init; }
    public string? Cidade { get; init; }
    public string? Uf { get; init; }
    public int? QuantidadeVidas { get; init; }
    public string? Observacao { get; init; }
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
    public string? OrigemCaptura { get; init; }
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
    public string? UtmTerm { get; init; }
    public string? UtmContent { get; init; }
    public string? Gclid { get; init; }
    public string? Fbclid { get; init; }
    public string? StatusEnvioExterno { get; init; }
    public int TentativasEnvioExterno { get; init; }
    public string? UltimoErroEnvioExterno { get; init; }
    public DateTime? EnviadoEm { get; init; }
    public string? ErroEnvio { get; init; }
    public OrigemLeadDto? Origem { get; init; }
    public IReadOnlyCollection<LogIntegracaoLeadDto> LogsIntegracao { get; init; } = Array.Empty<LogIntegracaoLeadDto>();
}
