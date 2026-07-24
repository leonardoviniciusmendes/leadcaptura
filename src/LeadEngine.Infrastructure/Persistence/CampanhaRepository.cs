using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class CampanhaRepository(LeadEngineDbContext context) : ICampanhaRepository
{
    public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        return context.Campanhas.AddAsync(campanha, cancellationToken).AsTask();
    }

    public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken)
    {
        return context.CampanhasRevisoes.AddAsync(revisao, cancellationToken).AsTask();
    }

    public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken)
    {
        return context.Campanhas.AnyAsync(x => x.Slug == slug && (ignorarId == null || x.Id != ignorarId), cancellationToken);
    }

    public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Campanhas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        return await context.CampanhasRevisoes
            .Where(x => x.CampanhaId == campanhaId)
            .OrderByDescending(x => x.DataAlteracao)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken)
    {
        return await context.Campanhas
            .OrderByDescending(x => x.DataCriacao)
            .ToArrayAsync(cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
