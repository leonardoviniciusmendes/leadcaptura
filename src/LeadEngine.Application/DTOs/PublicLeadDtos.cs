using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record CapturarLeadPublicoRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Cidade { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public int QuantidadeVidas { get; init; }
    public TipoContratacaoLead TipoContratacao { get; init; }
    public string? Observacao { get; init; }
    public bool Consentimento { get; init; }
    public string? Website { get; init; }
    public long? FormOpenedAt { get; init; }
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
    public string? UtmTerm { get; init; }
    public string? UtmContent { get; init; }
    public string? Gclid { get; init; }
    public string? Fbclid { get; init; }
}

public sealed record CapturarLeadPublicoResponse(
    Guid LeadId,
    string Mensagem,
    string WhatsAppUrl,
    bool ConversaoConfirmada);
