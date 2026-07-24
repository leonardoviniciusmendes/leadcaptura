using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class ConfiguracaoRepository(LeadEngineDbContext context) : IConfiguracaoRepository
{
    public async Task<IReadOnlyList<ConfiguracaoSistema>> ListarAsync(CancellationToken cancellationToken)
    {
        return await context.ConfiguracoesSistema.OrderBy(x => x.Categoria).ThenBy(x => x.Chave).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfiguracaoSistema>> ListarPorCategoriaAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
    {
        return await context.ConfiguracoesSistema.Where(x => x.Categoria == categoria).OrderBy(x => x.Chave).ToArrayAsync(cancellationToken);
    }

    public Task<ConfiguracaoSistema?> ObterPorChaveAsync(string chave, CancellationToken cancellationToken)
    {
        return context.ConfiguracoesSistema.FirstOrDefaultAsync(x => x.Chave == chave, cancellationToken);
    }

    public Task AdicionarAsync(ConfiguracaoSistema configuracao, CancellationToken cancellationToken)
    {
        return context.ConfiguracoesSistema.AddAsync(configuracao, cancellationToken).AsTask();
    }

    public Task AdicionarHistoricoAsync(ConfiguracaoSistemaHistorico historico, CancellationToken cancellationToken)
    {
        return context.ConfiguracoesSistemaHistorico.AddAsync(historico, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<ConfiguracaoSistemaHistorico>> ListarHistoricoAsync(CategoriaConfiguracao? categoria, string? chave, DateTime? dataInicial, DateTime? dataFinal, CancellationToken cancellationToken)
    {
        var query = context.ConfiguracoesSistemaHistorico.AsQueryable();
        if (categoria is not null) query = query.Where(x => x.Categoria == categoria.Value);
        if (!string.IsNullOrWhiteSpace(chave)) query = query.Where(x => x.Chave.Contains(chave));
        if (dataInicial is not null) query = query.Where(x => x.DataAlteracao >= dataInicial.Value);
        if (dataFinal is not null) query = query.Where(x => x.DataAlteracao <= dataFinal.Value);
        return await query.OrderByDescending(x => x.DataAlteracao).Take(200).ToArrayAsync(cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
