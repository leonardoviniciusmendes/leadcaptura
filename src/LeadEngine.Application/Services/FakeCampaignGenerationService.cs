using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class FakeCampaignGenerationService : ICampaignGenerationService
{
    public Task<CampaignGenerationResult> GenerateAsync(GerarCampanhaRequest briefing, CancellationToken cancellationToken)
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

        var result = new CampaignGenerationResult(
            nome,
            titulo,
            subtitulo,
            "Solicitar cotação pelo WhatsApp",
            mensagem,
            CampanhaText.Slugify(slugParts),
            ["Atendimento consultivo", "Cotação conforme perfil", "Comparação por região"],
            [
                new FaqItem("O valor é fixo?", "Não. Os preços variam por idade, região e tipo de contratação."),
                new FaqItem("A rede é garantida?", "Não. Rede e cobertura dependem do plano escolhido."),
                new FaqItem("Existe carência?", "A carência depende das condições da operadora e do contrato.")
            ],
            [$"plano de saúde {local}", $"cotação plano {publico.ToLowerInvariant()}", $"plano {operadora}"],
            ["emprego", "salário", "concurso", "segunda via", "boleto", "login"],
            ["Plano de saúde", $"Cotação em {local}", "Atendimento rápido", "Compare opções", "Fale no WhatsApp", "Plano por perfil", "Consultoria local", "Solicite cotação"],
            ["Receba atendimento para comparar opções conforme seu perfil.", "Informe seus dados e fale com uma consultoria especializada.", "Cotação orientada para planos de saúde na sua região."],
            "Fake",
            "fake-deterministic",
            0);

        return Task.FromResult(result);
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
