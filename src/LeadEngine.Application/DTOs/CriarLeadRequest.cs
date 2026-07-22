using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record CriarLeadRequest
{
    public TipoLead Tipo { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string WhatsApp { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Cep { get; init; }
    public string? Cidade { get; init; }
    public string? Uf { get; init; }
    public int? QuantidadeVidas { get; init; }
    public IReadOnlyCollection<int>? Idades { get; init; }
    public string? HospitalDesejado { get; init; }
    public string? OperadoraDesejada { get; init; }
    public bool? PossuiPlanoAtual { get; init; }
    public string? PlanoAtual { get; init; }
    public string? NomeEmpresa { get; init; }
    public string? Cnpj { get; init; }
    public int? QuantidadeFuncionarios { get; init; }
    public bool ConsentimentoContato { get; init; }
    public string? Honeypot { get; init; }
    public OrigemLeadDto? Origem { get; init; }
}
