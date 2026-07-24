using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Interfaces;

public interface ICampaignSectionGenerationService
{
    Task<CampaignSectionGenerationResult> GenerateAsync(
        Campanha campanha,
        CampanhaSecao secao,
        string? instrucaoAdicional,
        CancellationToken cancellationToken);
}

public sealed record CampaignSectionGenerationResult(
    CampanhaSecao Secao,
    object Conteudo,
    string Provider,
    string Modelo);
