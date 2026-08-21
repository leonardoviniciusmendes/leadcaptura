using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsContaRepository(LeadEngineDbContext context) : IMetaAdsContaRepository
{
    public Task<MetaAdsConta?> ObterAtivaAsync(CancellationToken cancellationToken)
    {
        return context.MetaAdsContas.FirstOrDefaultAsync(x => x.Ativa, cancellationToken);
    }

    public Task<MetaAdsConta?> ObterPorMetaUserIdAsync(string metaUserId, CancellationToken cancellationToken)
    {
        return context.MetaAdsContas.FirstOrDefaultAsync(x => x.MetaUserId == metaUserId, cancellationToken);
    }

    public Task AdicionarAsync(MetaAdsConta conta, CancellationToken cancellationToken)
    {
        return context.MetaAdsContas.AddAsync(conta, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
