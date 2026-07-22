namespace LeadEngine.Application.DTOs;

public sealed record ResultadoIntegracaoLead(
    bool Sucesso,
    int? StatusHttp,
    string? Mensagem,
    string? Endpoint);
