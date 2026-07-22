namespace LeadEngine.Application.DTOs;

public sealed record LogIntegracaoLeadDto(
    DateTime CriadoEm,
    bool Sucesso,
    int? StatusHttp,
    string? Mensagem,
    string? Endpoint);
