using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class SegmentRepository(LeadEngineDbContext context) : ISegmentRepository
{
    public Task<Segment?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return context.Segments.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public Task<Segment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Segments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Segment>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await context.Segments
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Segment>> ListAsync(CancellationToken cancellationToken)
    {
        return await context.Segments
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        return context.Segments.AnyAsync(
            x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<int> CountCampaignsAsync(Guid segmentId, CancellationToken cancellationToken)
    {
        return context.Campanhas.CountAsync(x => x.SegmentId == segmentId, cancellationToken);
    }

    public Task AddAsync(Segment segment, CancellationToken cancellationToken)
    {
        context.Segments.Add(segment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
