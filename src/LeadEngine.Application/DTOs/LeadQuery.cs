using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record LeadQuery(
    Guid? CampanhaId,
    DateTime? DataInicial,
    DateTime? DataFinal,
    TipoLead? Tipo,
    StatusLead? Status,
    string? Campanha,
    string? LandingPage,
    string? WhatsApp,
    TipoContratacaoLead? TipoContratacao,
    string? Origem,
    int Pagina = 1,
    int TamanhoPagina = 20);
