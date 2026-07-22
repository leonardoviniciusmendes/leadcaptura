namespace LeadEngine.Application.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina);
