using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record LeadResponse(
    Guid Id,
    TipoLead Tipo,
    StatusLead Status,
    string Nome,
    string WhatsAppMascarado,
    string? EmailMascarado,
    string? Cidade,
    string? Uf,
    string? LandingPage,
    string? UtmCampaign,
    DateTime CriadoEm,
    DateTime? EnviadoEm,
    string? ErroEnvio);
