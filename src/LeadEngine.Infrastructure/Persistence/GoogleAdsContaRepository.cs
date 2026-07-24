using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsContaRepository(LeadEngineDbContext context) : IGoogleAdsContaRepository
{
    public Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.GoogleAdsContas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken)
    {
        return context.GoogleAdsContas.FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
    }

    public Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken)
    {
        return context.GoogleAdsContas.FirstOrDefaultAsync(x => x.Padrao && x.Ativa, cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken)
    {
        return await context.GoogleAdsContas.OrderByDescending(x => x.Padrao).ThenBy(x => x.Nome).ToArrayAsync(cancellationToken);
    }

    public Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken)
    {
        return context.GoogleAdsContas.AddAsync(conta, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
