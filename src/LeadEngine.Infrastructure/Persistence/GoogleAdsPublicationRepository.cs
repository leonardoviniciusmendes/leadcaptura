using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsPublicationRepository(LeadEngineDbContext context) : IGoogleAdsPublicationRepository
{
    public Task<GoogleAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPublicacoes.Include(x => x.Recursos).Include(x => x.Operacoes).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<GoogleAdsPublicacao?> ObterPorPreviewVersaoHashAsync(Guid previewId, int versao, string hash, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPublicacoes.Include(x => x.Recursos).Include(x => x.Operacoes)
            .FirstOrDefaultAsync(x => x.GoogleAdsPlanoPublicacaoId == previewId && x.PreviewVersao == versao && x.PreviewHash == hash, cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsPublicacao>> ListarPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        return await context.GoogleAdsPublicacoes.Include(x => x.Recursos)
            .Where(x => x.CampanhaId == campanhaId)
            .OrderByDescending(x => x.DataCriacao)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsPublicacao>> ListarAsync(GoogleAdsPublicationQuery query, CancellationToken cancellationToken)
    {
        var q = context.GoogleAdsPublicacoes.Include(x => x.Recursos).AsQueryable();
        if (query.Status is not null) q = q.Where(x => x.Status == query.Status);
        if (query.CampanhaId is not null) q = q.Where(x => x.CampanhaId == query.CampanhaId);
        if (query.ContaId is not null) q = q.Where(x => x.GoogleAdsContaId == query.ContaId);
        if (query.DataInicial is not null) q = q.Where(x => x.DataCriacao >= query.DataInicial);
        if (query.DataFinal is not null) q = q.Where(x => x.DataCriacao <= query.DataFinal);
        return await q.OrderByDescending(x => x.DataCriacao).Take(200).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsPublicacaoHistorico>> ListarHistoricoAsync(Guid publicacaoId, CancellationToken cancellationToken)
    {
        return await context.GoogleAdsPublicacaoHistoricos
            .Where(x => x.GoogleAdsPublicacaoId == publicacaoId)
            .OrderBy(x => x.Data)
            .ToArrayAsync(cancellationToken);
    }

    public Task AdicionarAsync(GoogleAdsPublicacao publicacao, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPublicacoes.AddAsync(publicacao, cancellationToken).AsTask();
    }

    public Task AdicionarRecursoAsync(GoogleAdsRecursoPublicado recurso, CancellationToken cancellationToken)
    {
        return context.GoogleAdsRecursosPublicados.AddAsync(recurso, cancellationToken).AsTask();
    }

    public Task AdicionarHistoricoAsync(GoogleAdsPublicacaoHistorico historico, CancellationToken cancellationToken)
    {
        return context.GoogleAdsPublicacaoHistoricos.AddAsync(historico, cancellationToken).AsTask();
    }

    public Task AdicionarOperacaoAsync(GoogleAdsOperacaoPublicacao operacao, CancellationToken cancellationToken)
    {
        return context.GoogleAdsOperacoesPublicacao.AddAsync(operacao, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
