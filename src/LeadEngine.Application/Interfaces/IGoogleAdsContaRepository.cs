using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsContaRepository
{
    Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken);
    Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken);
    Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
