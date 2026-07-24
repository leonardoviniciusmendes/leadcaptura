using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IWhatsAppUrlBuilder
{
    string Build(Lead lead, Campanha campanha);
}
