using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface ICreativeAssetRepository
{
    Task<CreativeAsset?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreativeAsset>> ListarPorCampanhaAsync(Guid campaignId, CancellationToken cancellationToken);
    Task AdicionarAsync(CreativeAsset asset, CancellationToken cancellationToken);
    Task AdicionarAnaliseAsync(CreativeAssetAnalysis analysis, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
