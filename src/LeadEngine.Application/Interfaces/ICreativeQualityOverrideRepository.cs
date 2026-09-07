using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface ICreativeQualityOverrideRepository
{
    Task AdicionarAsync(CreativeQualityOverride item, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
