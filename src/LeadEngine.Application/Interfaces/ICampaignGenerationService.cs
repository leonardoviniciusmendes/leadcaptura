using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface ICampaignGenerationService
{
    CampaignGenerationResult Generate(GerarCampanhaRequest briefing);
}

public sealed record CampaignGenerationResult(
    string Nome,
    string TituloLandingPage,
    string SubtituloLandingPage,
    string TextoBotao,
    string MensagemWhatsApp,
    string Slug);
