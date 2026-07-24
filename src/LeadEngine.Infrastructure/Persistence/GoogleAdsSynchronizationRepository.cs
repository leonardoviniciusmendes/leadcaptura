using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsSynchronizationRepository(LeadEngineDbContext context) : IGoogleAdsSynchronizationRepository
{
    public Task AdicionarAsync(GoogleAdsSincronizacao sincronizacao, CancellationToken cancellationToken) => context.GoogleAdsSincronizacoes.AddAsync(sincronizacao, cancellationToken).AsTask();
    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
