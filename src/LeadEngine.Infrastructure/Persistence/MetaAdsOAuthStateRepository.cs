using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsOAuthStateRepository(LeadEngineDbContext context) : IMetaAdsOAuthStateRepository
{
    public Task AdicionarAsync(MetaAdsOAuthState state, CancellationToken cancellationToken)
    {
        return context.MetaAdsOAuthStates.AddAsync(state, cancellationToken).AsTask();
    }

    public Task<MetaAdsOAuthState?> ObterPorHashAsync(string stateHash, CancellationToken cancellationToken)
    {
        return context.MetaAdsOAuthStates.FirstOrDefaultAsync(x => x.StateHash == stateHash, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
