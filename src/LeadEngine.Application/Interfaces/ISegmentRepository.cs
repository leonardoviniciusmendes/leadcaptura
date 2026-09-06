using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface ISegmentRepository
{
    Task<Segment?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<Segment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Segment>> ListActiveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Segment>> ListAsync(CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<int> CountCampaignsAsync(Guid segmentId, CancellationToken cancellationToken);
    Task AddAsync(Segment segment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
