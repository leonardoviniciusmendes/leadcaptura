using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsSynchronizationRepository
{
    Task AdicionarAsync(GoogleAdsSincronizacao sincronizacao, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
