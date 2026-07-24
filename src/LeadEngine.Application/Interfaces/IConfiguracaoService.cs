using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Interfaces;

public interface IConfiguracaoService
{
    Task<IReadOnlyList<ConfiguracaoCategoriaResponse>> ListarAsync(CancellationToken cancellationToken);
    Task<ConfiguracaoCategoriaResponse> ObterCategoriaAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken);
    Task<ConfiguracaoCategoriaResponse> AtualizarCategoriaAsync(CategoriaConfiguracao categoria, Dictionary<string, object?> valores, CancellationToken cancellationToken);
    Task<TesteConfiguracaoResponse> TestarAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken);
    Task<ConfiguracoesStatusResponse> ObterStatusAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ConfiguracaoHistoricoResponse>> ListarHistoricoAsync(ConfiguracaoHistoricoQuery query, CancellationToken cancellationToken);
}
