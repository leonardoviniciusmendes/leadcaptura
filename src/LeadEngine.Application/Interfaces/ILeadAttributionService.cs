namespace LeadEngine.Application.Interfaces;

public interface ILeadAttributionService
{
    Task<int> AtribuirAsync(Guid? publicacaoId, CancellationToken cancellationToken);
}
