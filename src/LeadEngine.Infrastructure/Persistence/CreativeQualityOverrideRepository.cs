using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class CreativeQualityOverrideRepository(LeadEngineDbContext context) : ICreativeQualityOverrideRepository
{
    public Task AdicionarAsync(CreativeQualityOverride item, CancellationToken cancellationToken)
    {
        return context.CreativeQualityOverrides.AddAsync(item, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
