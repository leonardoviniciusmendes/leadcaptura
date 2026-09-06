using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class FakeCampaignGenerationService : ICampaignGenerationService
{
    public Task<CampaignGenerationResult> GenerateAsync(CampaignGenerationContext context, CancellationToken cancellationToken)
    {
        var result = Generate(context);
        return Task.FromResult(result);
    }

    private static CampaignGenerationResult Generate(CampaignGenerationContext context)
    {
        var local = Local(context);
        var productOrService = ProductOrService(context);
        var audience = TargetAudience(context);
        var goal = string.IsNullOrWhiteSpace(context.CampaignGoal) ? "captar leads qualificados" : context.CampaignGoal.Trim();
        var offer = string.IsNullOrWhiteSpace(context.Offer) ? "atendimento personalizado" : context.Offer.Trim();

        var nome = $"{productOrService} - {local}";
        var titulo = $"{productOrService} em {local}";
        var subtitulo = $"Receba {offer} para avaliar as opcoes disponiveis para {audience}.";
        var mensagem = $"Ola, gostaria de saber mais sobre {productOrService} em {local}.";
        var slug = CampanhaText.Slugify($"{productOrService}-{local}");
        var keywordBase = CampanhaText.Slugify(productOrService).Replace('-', ' ');

        return new CampaignGenerationResult(
            nome,
            titulo,
            subtitulo,
            CallToAction(context),
            mensagem,
            slug,
            ["Atendimento personalizado", "Orientacao conforme perfil", "Retorno pelo WhatsApp"],
            [
                new FaqItem("Como funciona o atendimento?", $"Voce informa seus dados e recebe contato para {goal}."),
                new FaqItem("As condicoes sao garantidas?", "Nao. Condicoes e disponibilidade dependem da avaliacao do fornecedor."),
                new FaqItem("Como recebo retorno?", "O contato e feito pelo WhatsApp com base nas informacoes enviadas.")
            ],
            [$"{keywordBase} {local}", $"{keywordBase} atendimento", $"{keywordBase} whatsapp"],
            ["emprego", "salario", "curso gratis", "segunda via", "boleto", "login"],
            [ShortTitle(productOrService), $"Atendimento {local}", "Fale no WhatsApp", "Solicite Contato", "Compare Opcoes", "Atendimento Local", "Receba Orientacao", "Avalie Alternativas"],
            ["Receba atendimento para avaliar opcoes conforme seu perfil.", "Informe seus dados e fale com um especialista.", $"Solicite contato sobre {productOrService}."],
            "Fake",
            "fake-deterministic",
            0);
    }

    private static string ProductOrService(CampaignGenerationContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.ProductOrService))
        {
            return context.ProductOrService.Trim();
        }

        var segment = LegacySingularLabel(context.SegmentName);
        var publico = PublicoLabel(context.LegacyContext.TipoPublico);
        var fornecedor = EffectiveSupplier(context);
        return string.IsNullOrWhiteSpace(fornecedor)
            ? $"{segment} {publico}".Trim()
            : $"{segment} {publico} {fornecedor}".Trim();
    }

    private static string LegacySingularLabel(string? segmentName)
    {
        if (string.IsNullOrWhiteSpace(segmentName))
        {
            return "Servico";
        }

        var firstWord = segmentName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Servico";
        return firstWord.EndsWith('s') && firstWord.Length > 1 ? firstWord[..^1] : firstWord;
    }

    private static string TargetAudience(CampaignGenerationContext context)
    {
        return string.IsNullOrWhiteSpace(context.TargetAudience)
            ? PublicoLabel(context.LegacyContext.TipoPublico).ToLowerInvariant()
            : context.TargetAudience.Trim();
    }

    private static string Local(CampaignGenerationContext context)
    {
        return !string.IsNullOrWhiteSpace(context.Location?.Region)
            ? context.Location.Region.Trim()
            : !string.IsNullOrWhiteSpace(context.Location?.City)
                ? context.Location.City.Trim()
                : "sua regiao";
    }

    private static string CallToAction(CampaignGenerationContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.CampaignGoal) && context.CampaignGoal.Contains("agend", StringComparison.OrdinalIgnoreCase))
        {
            return "Solicitar agendamento";
        }

        if (!string.IsNullOrWhiteSpace(context.TemplateKey) && context.TemplateKey.Contains("quote", StringComparison.OrdinalIgnoreCase))
        {
            return "Solicitar cotacao";
        }

        return "Solicitar contato";
    }

    private static string EffectiveSupplier(CampaignGenerationContext context)
    {
        var value = string.Equals(context.LegacyContext.Operadora, "Outra", StringComparison.OrdinalIgnoreCase)
            ? context.LegacyContext.OperadoraOutra
            : context.LegacyContext.Operadora;

        return string.Equals(value, "Nenhuma especifica", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Nenhuma específica", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : value?.Trim() ?? string.Empty;
    }

    private static string ShortTitle(string value)
    {
        return value.Length <= 30 ? value : value[..30].Trim();
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
            _ => "Publico"
        };
    }
}
