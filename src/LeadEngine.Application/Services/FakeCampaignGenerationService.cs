using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class FakeCampaignGenerationService : ICampaignGenerationService
{
    public CampaignGenerationResult Generate(GerarCampanhaRequest briefing)
    {
        var operadora = CampanhaValidator.OperadoraEfetiva(briefing);
        var publico = PublicoLabel(briefing.TipoPublico);
        var local = string.IsNullOrWhiteSpace(briefing.Regiao)
            ? briefing.Cidade.Trim()
            : briefing.Regiao.Trim();

        var usaOperadora = !string.Equals(operadora, "Nenhuma específica", StringComparison.OrdinalIgnoreCase);
        var nome = usaOperadora
            ? $"Plano {publico} {operadora} - {local}"
            : $"Plano {publico} - {local}";

        var titulo = $"Plano de Saúde {publico} em {local}";
        var subtitulo = briefing.TipoPublico is TipoPublicoCampanha.Empresa or TipoPublicoCampanha.Mei
            ? "Compare opções para sua empresa com atendimento personalizado."
            : "Compare opções para seu perfil com atendimento personalizado.";

        var mensagem = $"Olá, gostaria de uma cotação de plano de saúde {publico.ToLowerInvariant()} em {local}.";
        if (usaOperadora)
        {
            mensagem += $" Tenho interesse em {operadora}.";
        }

        var slugParts = usaOperadora
            ? $"plano-{publico}-{operadora}-{local}"
            : $"plano-{publico}-{local}";

        return new CampaignGenerationResult(
            nome,
            titulo,
            subtitulo,
            "Solicitar cotação pelo WhatsApp",
            mensagem,
            CampanhaText.Slugify(slugParts));
    }

    private static string PublicoLabel(TipoPublicoCampanha tipo)
    {
        return tipo switch
        {
            TipoPublicoCampanha.Individual => "Individual",
            TipoPublicoCampanha.Casal => "Casal",
            TipoPublicoCampanha.Familia => "Familiar",
            TipoPublicoCampanha.Mei => "MEI",
            TipoPublicoCampanha.Empresa => "Empresarial",
            _ => "Plano"
        };
    }
}
