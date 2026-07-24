using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsOAuthStateRepository(LeadEngineDbContext context) : IGoogleAdsOAuthStateRepository
{
    public Task AdicionarAsync(GoogleAdsOAuthState state, CancellationToken cancellationToken)
    {
        return context.GoogleAdsOAuthStates.AddAsync(state, cancellationToken).AsTask();
    }

    public Task<GoogleAdsOAuthState?> ObterPorHashAsync(string stateHash, CancellationToken cancellationToken)
    {
        return context.GoogleAdsOAuthStates.FirstOrDefaultAsync(x => x.StateHash == stateHash, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
