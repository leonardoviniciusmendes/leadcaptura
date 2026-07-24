using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsAnalysisRepository(LeadEngineDbContext context) : IGoogleAdsAnalysisRepository
{
    public Task AdicionarAsync(GoogleAdsAnaliseIa analise, CancellationToken cancellationToken) => context.GoogleAdsAnalisesIa.AddAsync(analise, cancellationToken).AsTask();
    public Task<GoogleAdsAnaliseIa?> ObterAsync(Guid id, CancellationToken cancellationToken) => context.GoogleAdsAnalisesIa.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<GoogleAdsAnaliseIa>> ListarPorPublicacaoAsync(Guid publicacaoId, CancellationToken cancellationToken) => await context.GoogleAdsAnalisesIa.Where(x => x.GoogleAdsPublicacaoId == publicacaoId).OrderByDescending(x => x.DataCriacao).ToArrayAsync(cancellationToken);
    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
