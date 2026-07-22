using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface ILeadRepository
{
    Task<Lead?> ObterDuplicadoRecenteAsync(string whatsAppNormalizado, DateTime criadoApos, CancellationToken cancellationToken);
    Task<Lead?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Lead>> ListarAsync(LeadQuery query, CancellationToken cancellationToken);
    Task AdicionarAsync(Lead lead, CancellationToken cancellationToken);
    Task AdicionarTentativaAsync(TentativaCapturaLead tentativa, CancellationToken cancellationToken);
    Task AdicionarLogIntegracaoAsync(LogIntegracaoLead log, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
