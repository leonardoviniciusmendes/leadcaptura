using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsValidationService : IGoogleAdsValidationService
{
    public GoogleAdsValidationResult ValidarEntrada(Campanha? campanha, GoogleAdsConta? conta, GoogleAdsConfigurationSnapshot config)
    {
        var erros = new List<string>();
        var avisos = new List<string>();

        if (campanha is null)
        {
            erros.Add("Campanha inexistente.");
            return new GoogleAdsValidationResult(erros, avisos);
        }

        if (campanha.Status != StatusCampanha.Revisada && campanha.Status != StatusCampanha.Publicada)
        {
            erros.Add("Campanha precisa estar aprovada/revisada.");
        }
        if (!campanha.Publicada || !campanha.Ativo)
        {
            erros.Add("Landing precisa estar publicada e ativa.");
        }
        if (string.IsNullOrWhiteSpace(campanha.Slug))
        {
            erros.Add("Campanha precisa ter slug publico.");
        }
        var urlResult = CampaignPublicUrlBuilder.Build(campanha.Slug, config.PublicBaseUrl, campanha.UrlPublica);
        if (!urlResult.Valida || !IsAbsoluteUrl(urlResult.Url))
        {
            erros.Add("URL publica da landing invalida.");
        }
        if (conta is null)
        {
            erros.Add("Selecione uma conta Google Ads padrao.");
        }
        if (!config.ConfiguracaoValida)
        {
            erros.Add("Configuracao Google Ads invalida.");
        }
        if (campanha.OrcamentoDiario <= 0 && config.DefaultDailyBudget <= 0)
        {
            erros.Add("Orcamento diario deve ser maior que zero.");
        }

        return new GoogleAdsValidationResult(erros, avisos);
    }

    public GoogleAdsValidationResult ValidarPayload(GoogleAdsPreviewPayload payload, GoogleAdsConfigurationSnapshot config)
    {
        var erros = new List<string>();
        var avisos = new List<string>();
        var adGroup = payload.AdGroups.FirstOrDefault();
        var rsa = adGroup?.ResponsiveSearchAd;

        if (payload.Budget.Amount <= 0)
        {
            erros.Add("Orcamento diario deve ser maior que zero.");
        }
        if (string.IsNullOrWhiteSpace(payload.Campaign.CurrencyCode))
        {
            erros.Add("Moeda ausente.");
        }
        if (!IsAbsoluteUrl(payload.Campaign.FinalUrl))
        {
            erros.Add("URL final invalida.");
        }
        if (rsa is null)
        {
            erros.Add("Anuncio responsivo ausente.");
        }
        else
        {
            if (rsa.Headlines.Count < 3 || rsa.Headlines.Count > 15)
            {
                erros.Add("Responsive Search Ad deve ter entre 3 e 15 headlines.");
            }
            if (rsa.Descriptions.Count < 2 || rsa.Descriptions.Count > 4)
            {
                erros.Add("Responsive Search Ad deve ter entre 2 e 4 descriptions.");
            }
            if (rsa.Headlines.Any(x => x.Length > 30))
            {
                erros.Add("Headline acima de 30 caracteres.");
            }
            if (rsa.Descriptions.Any(x => x.Length > 90))
            {
                erros.Add("Description acima de 90 caracteres.");
            }
            if (rsa.Path1.Length > 15 || rsa.Path2.Length > 15)
            {
                erros.Add("Path1 e Path2 devem ter no maximo 15 caracteres.");
            }
            if (rsa.Headlines.Count != rsa.Headlines.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                avisos.Add("Existem headlines repetidas.");
            }
            if (SimilarDescriptions(rsa.Descriptions))
            {
                avisos.Add("Existem descricoes semelhantes.");
            }
        }

        if (adGroup is null || adGroup.Keywords.Count == 0)
        {
            erros.Add("Ad group precisa ter pelo menos uma keyword.");
        }
        else
        {
            ValidateKeywords(adGroup.Keywords.Select(x => x.Text).ToArray(), "Keyword", erros);
            ValidateKeywords(adGroup.NegativeKeywords.Select(x => x.Text).ToArray(), "Keyword negativa", erros);
            if (adGroup.Keywords.Count < 5)
            {
                avisos.Add("Poucas palavras-chave planejadas.");
            }
            if (!adGroup.Keywords.Any(x => string.Equals(x.MatchType, "EXACT", StringComparison.OrdinalIgnoreCase)))
            {
                avisos.Add("Nenhuma keyword Exact planejada.");
            }
            if (adGroup.NegativeKeywords.Count == 0)
            {
                avisos.Add("Nenhuma palavra-chave negativa planejada.");
            }
            if (adGroup.CpcBid is null)
            {
                avisos.Add("CPC inicial nao configurado.");
            }
        }

        if (payload.Budget.Amount < 10)
        {
            avisos.Add("Orcamento diario baixo.");
        }
        if (!string.IsNullOrWhiteSpace(config.PublicBaseUrl)
            && IsAbsoluteUrl(payload.Campaign.FinalUrl)
            && !payload.Campaign.FinalUrl.StartsWith(config.PublicBaseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            avisos.Add("URL final nao usa o dominio publico configurado.");
        }

        return new GoogleAdsValidationResult(erros, avisos);
    }

    private static void ValidateKeywords(IReadOnlyList<string> values, string label, ICollection<string> erros)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            erros.Add($"{label} nao pode estar vazia.");
        }
        if (values.Any(x => x.Length > 80))
        {
            erros.Add($"{label} deve ter no maximo 80 caracteres.");
        }
        if (values.Any(x => !x.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c is '-' or '+' or '&')))
        {
            erros.Add($"{label} contem caracteres invalidos.");
        }
        if (values.Count != values.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            erros.Add($"{label} nao deve conter duplicatas.");
        }
    }

    private static bool IsAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }

    private static bool SimilarDescriptions(IReadOnlyList<string> descriptions)
    {
        var normalized = descriptions.Select(x => x.Trim().ToLowerInvariant()).ToArray();
        for (var i = 0; i < normalized.Length; i++)
        {
            for (var j = i + 1; j < normalized.Length; j++)
            {
                if (normalized[i].Length > 12 && normalized[j].Contains(normalized[i][..Math.Min(20, normalized[i].Length)], StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
