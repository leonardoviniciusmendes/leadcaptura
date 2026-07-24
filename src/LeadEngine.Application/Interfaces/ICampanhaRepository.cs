using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface ICampanhaRepository
{
    Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken);
    Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken);
    Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken);
    Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
