using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Application.Services;

public sealed class LeadConsultaService(ILeadRepository repository)
{
    public async Task<PagedResult<LeadResponse>> ListarAsync(LeadQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.ListarAsync(query, cancellationToken);
        return new PagedResult<LeadResponse>(
            result.Itens.Select(LeadMapping.ToResponse).ToArray(),
            result.Total,
            result.Pagina,
            result.TamanhoPagina);
    }

    public async Task<LeadDetalheResponse?> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        var lead = await repository.ObterPorIdAsync(id, cancellationToken);
        return lead is null ? null : LeadMapping.ToDetalhe(lead);
    }
}
