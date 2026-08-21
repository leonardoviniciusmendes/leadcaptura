using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsAtivoSelecionadoRepository
{
    Task<MetaAdsAtivoSelecionado?> ObterPorContaIdAsync(Guid contaId, CancellationToken cancellationToken);
    Task AdicionarAsync(MetaAdsAtivoSelecionado selecao, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
