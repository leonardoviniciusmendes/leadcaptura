using System.Globalization;
using System.Diagnostics;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsPreviewService(
    ICampanhaRepository campanhaRepository,
    IGoogleAdsContaRepository contaRepository,
    IGoogleAdsPlanoPublicacaoRepository planoRepository,
    IGoogleAdsCampaignMappingService mappingService,
    IGoogleAdsValidationService validationService,
    IGoogleAdsCopyAdjustmentService copyAdjustmentService,
    IConfigurationResolver resolver) : IGoogleAdsPreviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsPreviewResponse> GerarOuAtualizarAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPorIdAsync(campanhaId, cancellationToken);
        var conta = await contaRepository.ObterPadraoAsync(cancellationToken);
        var config = await Config(cancellationToken);
        var urlDiagnostics = CampaignPublicUrlBuilder.Build(campanha?.Slug, config.PublicBaseUrl, campanha?.UrlPublica);
        Trace.TraceInformation(
            "Google Ads preview landing URL diagnostics. PublicBaseUrl={0}; Slug={1}; UrlConstruida={2}; UrlPersistida={3}",
            urlDiagnostics.PublicBaseUrl,
            urlDiagnostics.Slug,
            urlDiagnostics.Url,
            urlDiagnostics.PersistedUrl);
        if (!urlDiagnostics.Valida)
        {
            Trace.TraceWarning(
                "Google Ads preview landing URL validation failed. PublicBaseUrl={0}; Slug={1}; UrlConstruida={2}; Motivo={3}",
                urlDiagnostics.PublicBaseUrl,
                urlDiagnostics.Slug,
                urlDiagnostics.Url,
                urlDiagnostics.MotivoFalha);
        }

        var entrada = validationService.ValidarEntrada(campanha, conta, config);
        if (campanha is null || conta is null || entrada.Erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", entrada.Erros));
        }

        var payload = await mappingService.MapearAsync(campanha, cancellationToken);
        var validacao = validationService.ValidarPayload(payload, config);
        if (validacao.Erros.Any(x => x.Contains("URL", StringComparison.OrdinalIgnoreCase)))
        {
            Trace.TraceWarning(
                "Google Ads preview payload URL validation failed. PublicBaseUrl={0}; Slug={1}; UrlConstruida={2}; Motivo={3}",
                config.PublicBaseUrl,
                campanha.Slug,
                payload.Campaign.FinalUrl,
                string.Join(" ", validacao.Erros.Where(x => x.Contains("URL", StringComparison.OrdinalIgnoreCase))));
        }
        var existing = await planoRepository.ObterPorCampanhaIdAsync(campanhaId, cancellationToken);
        var now = DateTime.UtcNow;
        var hash = mappingService.CalcularHash(campanha);
        var plano = existing ?? new GoogleAdsPlanoPublicacao
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanha.Id,
            DataCriacao = now,
            Versao = 0
        };

        if (existing is not null)
        {
            payload = PreservePreviewOverrides(payload, DeserializePayload(existing.PayloadPreviewJson));
            validacao = validationService.ValidarPayload(payload, config);
        }

        if (existing is null)
        {
            await planoRepository.AdicionarAsync(plano, cancellationToken);
        }

        plano.GoogleAdsContaId = conta.Id;
        plano.NomeCampanha = payload.Campaign.Name;
        plano.Objetivo = payload.Campaign.Objective;
        plano.Status = validacao.Valido ? StatusPlanoPublicacaoGoogleAds.Valido : StatusPlanoPublicacaoGoogleAds.Invalido;
        plano.TipoRede = payload.Campaign.AdvertisingChannelType;
        plano.OrcamentoDiario = payload.Budget.Amount;
        plano.CodigoMoeda = payload.Campaign.CurrencyCode;
        plano.Idioma = payload.Campaign.LanguageCode;
        plano.Pais = payload.Campaign.CountryCode;
        plano.UrlFinal = payload.Campaign.FinalUrl;
        plano.DataAtualizacao = now;
        plano.DataValidacao = now;
        plano.ErrosValidacaoJson = Serialize(validacao.Erros);
        plano.AvisosValidacaoJson = Serialize(validacao.Avisos.Concat(entrada.Avisos).Distinct().ToArray());
        plano.PayloadPreviewJson = Serialize(payload);
        plano.ConteudoHash = hash;
        plano.Versao++;

        await planoRepository.SalvarAsync(cancellationToken);
        return ToResponse(plano, campanha, conta, payload);
    }

    public async Task<GoogleAdsPreviewResponse> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        return await ResponseAsync(plano, cancellationToken);
    }

    public async Task<GoogleAdsPreviewResponse> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorCampanhaIdAsync(campanhaId, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        return await ResponseAsync(plano, cancellationToken);
    }

    public async Task<GoogleAdsPreviewResponse> ValidarAsync(Guid id, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        var config = await Config(cancellationToken);
        var payload = DeserializePayload(plano.PayloadPreviewJson);
        var validacao = validationService.ValidarPayload(payload, config);
        plano.Status = validacao.Valido ? StatusPlanoPublicacaoGoogleAds.Valido : StatusPlanoPublicacaoGoogleAds.Invalido;
        plano.ErrosValidacaoJson = Serialize(validacao.Erros);
        plano.AvisosValidacaoJson = Serialize(validacao.Avisos);
        plano.DataValidacao = DateTime.UtcNow;
        plano.DataAtualizacao = DateTime.UtcNow;
        await planoRepository.SalvarAsync(cancellationToken);
        return await ResponseAsync(plano, cancellationToken);
    }

    public async Task<GoogleAdsPreviewResponse> AtualizarAsync(Guid id, AtualizarGoogleAdsPreviewRequest request, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        var campanha = await campanhaRepository.ObterPorIdAsync(plano.CampanhaId, cancellationToken);
        var payload = DeserializePayload(plano.PayloadPreviewJson);
        var mappedCampaign = campanha is null ? payload.Campaign : (await mappingService.MapearAsync(campanha, cancellationToken)).Campaign;
        var adGroup = payload.AdGroups.First();
        var rsa = adGroup.ResponsiveSearchAd;
        var budget = request.OrcamentoDiario ?? payload.Budget.Amount;
        var cpc = request.CpcBid ?? adGroup.CpcBid;
        var updatedPayload = payload with
        {
            Campaign = payload.Campaign with
            {
                Name = request.NomeCampanha ?? payload.Campaign.Name,
                LocationName = mappedCampaign.LocationName,
                GeoTargetResourceName = mappedCampaign.GeoTargetResourceName
            },
            Budget = payload.Budget with { Amount = budget, AmountMicros = GoogleAdsMoney.ToMicros(budget) },
            AdGroups =
            [
                adGroup with
                {
                    Name = request.NomeGrupo ?? adGroup.Name,
                    CpcBid = cpc,
                    CpcBidMicros = cpc is null ? null : GoogleAdsMoney.ToMicros(cpc.Value),
                    Keywords = request.Keywords is null ? adGroup.Keywords : request.Keywords.Select(x => new GoogleAdsKeywordPlan(x.Texto.Trim(), NormalizeMatch(x.MatchType), "PAUSED", "Manual")).ToArray(),
                    NegativeKeywords = request.Negativas is null ? adGroup.NegativeKeywords : request.Negativas.Select(x => new GoogleAdsNegativeKeywordPlan(x.Texto.Trim(), NormalizeMatch(x.MatchType), "Manual")).ToArray(),
                    ResponsiveSearchAd = rsa with
                    {
                        Headlines = request.Headlines ?? rsa.Headlines,
                        Descriptions = request.Descriptions ?? rsa.Descriptions,
                        Path1 = request.Path1 ?? rsa.Path1,
                        Path2 = request.Path2 ?? rsa.Path2
                    }
                }
            ]
        };

        await SavePayloadAsync(plano, updatedPayload, cancellationToken);
        return await ResponseAsync(plano, cancellationToken);
    }

    public async Task<GoogleAdsCopySuggestionResponse> SugerirAjustesAsync(Guid id, GoogleAdsSugerirAjustesRequest request, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        var payload = DeserializePayload(plano.PayloadPreviewJson);
        var campos = request.Campos is { Count: > 0 } ? request.Campos : ["headlines", "descriptions"];
        var sugestoes = await copyAdjustmentService.SugerirAsync(payload, campos, cancellationToken);
        return new GoogleAdsCopySuggestionResponse(id, sugestoes);
    }

    public async Task<GoogleAdsPreviewResponse> AplicarSugestaoAsync(Guid id, AplicarGoogleAdsSugestaoRequest request, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        var payload = DeserializePayload(plano.PayloadPreviewJson);
        var adGroup = payload.AdGroups.First();
        var rsa = adGroup.ResponsiveSearchAd;
        var field = request.Campo.Trim().ToLowerInvariant();
        IReadOnlyList<string> headlines = rsa.Headlines;
        IReadOnlyList<string> descriptions = rsa.Descriptions;

        if (field is "headline" or "headlines")
        {
            headlines = ReplaceAt(rsa.Headlines, request.Indice, request.Sugestao);
        }
        else if (field is "description" or "descriptions")
        {
            descriptions = ReplaceAt(rsa.Descriptions, request.Indice, request.Sugestao);
        }
        else
        {
            throw new ArgumentException("Campo de sugestao invalido.");
        }

        var updatedPayload = payload with
        {
            AdGroups = [adGroup with { ResponsiveSearchAd = rsa with { Headlines = headlines, Descriptions = descriptions } }]
        };
        await SavePayloadAsync(plano, updatedPayload, cancellationToken);
        return await ResponseAsync(plano, cancellationToken);
    }

    public async Task<GoogleAdsPreviewPayload> ObterPayloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        return DeserializePayload(plano.PayloadPreviewJson);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var plano = await planoRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        await planoRepository.RemoverAsync(plano, cancellationToken);
        await planoRepository.SalvarAsync(cancellationToken);
    }

    private async Task SavePayloadAsync(GoogleAdsPlanoPublicacao plano, GoogleAdsPreviewPayload payload, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        var validacao = validationService.ValidarPayload(payload, config);
        plano.NomeCampanha = payload.Campaign.Name;
        plano.Objetivo = payload.Campaign.Objective;
        plano.Status = validacao.Valido ? StatusPlanoPublicacaoGoogleAds.Valido : StatusPlanoPublicacaoGoogleAds.Invalido;
        plano.OrcamentoDiario = payload.Budget.Amount;
        plano.CodigoMoeda = payload.Campaign.CurrencyCode;
        plano.Idioma = payload.Campaign.LanguageCode;
        plano.Pais = payload.Campaign.CountryCode;
        plano.UrlFinal = payload.Campaign.FinalUrl;
        plano.PayloadPreviewJson = Serialize(payload);
        plano.ErrosValidacaoJson = Serialize(validacao.Erros);
        plano.AvisosValidacaoJson = Serialize(validacao.Avisos);
        plano.DataAtualizacao = DateTime.UtcNow;
        plano.DataValidacao = DateTime.UtcNow;
        plano.Versao++;
        await planoRepository.SalvarAsync(cancellationToken);
    }

    private async Task<GoogleAdsPreviewResponse> ResponseAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPorIdAsync(plano.CampanhaId, cancellationToken);
        var conta = await contaRepository.ObterPorIdAsync(plano.GoogleAdsContaId, cancellationToken);
        if (campanha is not null && plano.ConteudoHash != mappingService.CalcularHash(campanha) && plano.Status != StatusPlanoPublicacaoGoogleAds.Desatualizado)
        {
            plano.Status = StatusPlanoPublicacaoGoogleAds.Desatualizado;
            plano.DataAtualizacao = DateTime.UtcNow;
            await planoRepository.SalvarAsync(cancellationToken);
        }
        return ToResponse(plano, campanha, conta, DeserializePayload(plano.PayloadPreviewJson));
    }

    private GoogleAdsPreviewResponse ToResponse(GoogleAdsPlanoPublicacao plano, Campanha? campanha, GoogleAdsConta? conta, GoogleAdsPreviewPayload payload)
    {
        var erros = DeserializeList(plano.ErrosValidacaoJson);
        var avisos = DeserializeList(plano.AvisosValidacaoJson);
        var rsa = payload.AdGroups.FirstOrDefault()?.ResponsiveSearchAd;
        var adGroup = payload.AdGroups.FirstOrDefault();
        return new GoogleAdsPreviewResponse(
            plano.Id,
            plano.CampanhaId,
            plano.GoogleAdsContaId,
            conta?.Nome ?? "Conta nao encontrada",
            conta?.CustomerId ?? string.Empty,
            plano.NomeCampanha,
            plano.Objetivo,
            plano.Status,
            plano.TipoRede,
            plano.OrcamentoDiario,
            payload.Budget.AmountMicros,
            plano.CodigoMoeda,
            plano.Idioma,
            plano.Pais,
            plano.UrlFinal,
            plano.DataCriacao,
            plano.DataAtualizacao,
            plano.DataValidacao,
            plano.Versao,
            erros,
            avisos,
            campanha is not null && plano.ConteudoHash != mappingService.CalcularHash(campanha),
            payload,
            new GoogleAdsPreviewCounters(
                rsa?.Headlines.Count(x => x.Length <= 30) ?? 0,
                rsa?.Descriptions.Count(x => x.Length <= 90) ?? 0,
                adGroup?.Keywords.Count ?? 0,
                adGroup?.NegativeKeywords.Count ?? 0,
                erros.Count,
                avisos.Count));
    }

    private static GoogleAdsPreviewPayload PreservePreviewOverrides(GoogleAdsPreviewPayload mappedPayload, GoogleAdsPreviewPayload existingPayload)
    {
        var mappedAdGroup = mappedPayload.AdGroups.FirstOrDefault();
        var existingAdGroup = existingPayload.AdGroups.FirstOrDefault();
        var preservedPayload = mappedPayload with
        {
            Campaign = mappedPayload.Campaign with { Name = existingPayload.Campaign.Name }
        };

        if (mappedAdGroup is null || existingAdGroup is null || existingAdGroup.CpcBid is null)
        {
            return preservedPayload;
        }

        return preservedPayload with
        {
            AdGroups =
            [
                mappedAdGroup with
                {
                    CpcBid = existingAdGroup.CpcBid,
                    CpcBidMicros = existingAdGroup.CpcBidMicros ?? GoogleAdsMoney.ToMicros(existingAdGroup.CpcBid.Value)
                }
            ]
        };
    }

    private async Task<GoogleAdsConfigurationSnapshot> Config(CancellationToken cancellationToken)
    {
        var clientId = await Value(CategoriaConfiguracao.GoogleAds, "ClientId", cancellationToken);
        var clientSecret = await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ClientSecret", cancellationToken);
        var developerToken = await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "DeveloperToken", cancellationToken);
        var publicUrl = (await new CampaignPublicUrlBuilder(resolver).BuildAsync("diagnostico", null, cancellationToken)).PublicBaseUrl;
        var defaultBudget = decimal.TryParse(await Value(CategoriaConfiguracao.GoogleAds, "DefaultDailyBudget", cancellationToken), NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) ? budget : 10m;
        var defaultCpc = decimal.TryParse(await Value(CategoriaConfiguracao.GoogleAds, "DefaultCpcBid", cancellationToken), NumberStyles.Number, CultureInfo.InvariantCulture, out var cpc) ? cpc : (decimal?)null;
        return new GoogleAdsConfigurationSnapshot(
            !string.IsNullOrWhiteSpace(clientId) && clientSecret.Configured && developerToken.Configured,
            defaultBudget,
            await Value(CategoriaConfiguracao.GoogleAds, "DefaultCountryCode", cancellationToken) ?? "BR",
            await Value(CategoriaConfiguracao.GoogleAds, "DefaultLanguageCode", cancellationToken) ?? "pt",
            await Value(CategoriaConfiguracao.GoogleAds, "DefaultCurrencyCode", cancellationToken) ?? "BRL",
            await Value(CategoriaConfiguracao.GoogleAds, "DefaultKeywordMatchType", cancellationToken) ?? "Phrase",
            await Value(CategoriaConfiguracao.GoogleAds, "DefaultCampaignStatus", cancellationToken) ?? "PAUSED",
            bool.TryParse(await Value(CategoriaConfiguracao.GoogleAds, "EnableBroadMatch", cancellationToken), out var broad) && broad,
            defaultCpc,
            publicUrl);
    }

    private async Task<string?> Value(CategoriaConfiguracao categoria, string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(categoria, key, cancellationToken)).Value;
    }

    private static string NormalizeMatch(string? matchType)
    {
        return (matchType ?? "PHRASE").Trim().ToUpperInvariant() switch
        {
            "BROAD" or "BROAD_MATCH" => "BROAD",
            "EXACT" => "EXACT",
            _ => "PHRASE"
        };
    }

    private static IReadOnlyList<string> ReplaceAt(IReadOnlyList<string> values, int index, string value)
    {
        if (index < 0 || index >= values.Count)
        {
            throw new ArgumentException("Indice de sugestao invalido.");
        }
        var copy = values.ToArray();
        copy[index] = value;
        return copy;
    }

    private static GoogleAdsPreviewPayload DeserializePayload(string json) => JsonSerializer.Deserialize<GoogleAdsPreviewPayload>(json, JsonOptions) ?? throw new InvalidOperationException("Payload do preview invalido.");
    private static IReadOnlyList<string> DeserializeList(string json) => JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions) ?? [];
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}
