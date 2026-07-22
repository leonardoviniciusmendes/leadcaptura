using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IIntegracaoLeadService
{
    Task<ResultadoIntegracaoLead> EnviarAsync(Lead lead, CancellationToken cancellationToken);
}
