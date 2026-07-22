namespace LeadEngine.Application.DTOs;

public sealed record CapturaLeadResponse(
    bool Sucesso,
    Guid LeadId,
    string Mensagem,
    bool Duplicado = false);
