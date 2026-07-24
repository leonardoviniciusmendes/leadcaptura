using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsPlanoPublicacaoRepository(LeadEngineDbContext context) : IGoogleAdsPlanoPublicacaoRepository
{
    public Task<GoogleAdsPlanoPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPlanosPublicacao
            .Include(x => x.Campanha)
            .Include(x => x.GoogleAdsConta)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<GoogleAdsPlanoPublicacao?> ObterPorCampanhaIdAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPlanosPublicacao
            .Include(x => x.Campanha)
            .Include(x => x.GoogleAdsConta)
            .FirstOrDefaultAsync(x => x.CampanhaId == campanhaId, cancellationToken);
    }

    public Task AdicionarAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPlanosPublicacao.AddAsync(plano, cancellationToken).AsTask();
    }

    public Task RemoverAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken)
    {
        context.GoogleAdsPlanosPublicacao.Remove(plano);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
