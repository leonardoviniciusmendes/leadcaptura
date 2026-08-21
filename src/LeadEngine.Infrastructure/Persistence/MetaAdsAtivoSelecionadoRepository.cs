using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsAtivoSelecionadoRepository(LeadEngineDbContext context) : IMetaAdsAtivoSelecionadoRepository
{
    public Task<MetaAdsAtivoSelecionado?> ObterPorContaIdAsync(Guid contaId, CancellationToken cancellationToken)
    {
        return context.MetaAdsAtivosSelecionados.FirstOrDefaultAsync(x => x.MetaAdsContaId == contaId, cancellationToken);
    }

    public Task AdicionarAsync(MetaAdsAtivoSelecionado selecao, CancellationToken cancellationToken)
    {
        return context.MetaAdsAtivosSelecionados.AddAsync(selecao, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
