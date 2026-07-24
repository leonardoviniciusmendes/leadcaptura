using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record LeadResponse(
    Guid Id,
    Guid? CampanhaId,
    string? CampanhaNome,
    TipoLead Tipo,
    TipoContratacaoLead? TipoContratacao,
    StatusLead Status,
    string Nome,
    string WhatsAppMascarado,
    string? EmailMascarado,
    string? Cidade,
    string? Uf,
    int? QuantidadeVidas,
    string? Origem,
    string? LandingPage,
    string? UtmCampaign,
    DateTime CriadoEm,
    DateTime? EnviadoEm,
    string? ErroEnvio,
    string? StatusEnvioExterno);
