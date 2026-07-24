using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Interfaces;

public interface IConfiguracaoRepository
{
    Task<IReadOnlyList<ConfiguracaoSistema>> ListarAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ConfiguracaoSistema>> ListarPorCategoriaAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken);
    Task<ConfiguracaoSistema?> ObterPorChaveAsync(string chave, CancellationToken cancellationToken);
    Task AdicionarAsync(ConfiguracaoSistema configuracao, CancellationToken cancellationToken);
    Task AdicionarHistoricoAsync(ConfiguracaoSistemaHistorico historico, CancellationToken cancellationToken);
    Task<IReadOnlyList<ConfiguracaoSistemaHistorico>> ListarHistoricoAsync(CategoriaConfiguracao? categoria, string? chave, DateTime? dataInicial, DateTime? dataFinal, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
