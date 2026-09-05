using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeadEngine.Application.Services;

public sealed class MetaAdsPublishingService(
    ICampanhaRepository campanhaRepository,
    IMetaAdsContaRepository contaRepository,
    IMetaAdsAtivoSelecionadoRepository selecaoRepository,
    IMetaAdsPublicacaoRepository publicacaoRepository,
    IMetaAdsGraphClient graphClient,
    IMetaAdsPreviewService previewService,
    IConfigurationResolver resolver,
    ISecretProtector protector,
    ILogger<MetaAdsPublishingService> logger) : IMetaAdsPublishingService
{
    private const string Paused = "PAUSED";
    private static readonly TimeSpan FailurePersistenceTimeout = TimeSpan.FromSeconds(5);

    public async Task<MetaAdsPublicationStatusResponse> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterAtivaAsync(cancellationToken);
        if (conta is null)
        {
            return new MetaAdsPublicationStatusResponse(false, null);
        }

        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken);
        if (string.IsNullOrWhiteSpace(selecao?.AdAccountId))
        {
            return new MetaAdsPublicationStatusResponse(false, null);
        }

        var publicacao = await publicacaoRepository.ObterPorCampanhaAdAccountAsync(campanhaId, selecao.AdAccountId, cancellationToken);
        return new MetaAdsPublicationStatusResponse(publicacao is not null, publicacao is null ? null : ToResponse(publicacao));
    }

    public async Task<MetaAdsPublicacaoResponse> PublicarAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        var (conta, token, config, selecao) = await ContextAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(selecao.AdAccountId))
        {
            throw new InvalidOperationException("Selecione uma Ad Account antes de publicar.");
        }

        var publicacao = await publicacaoRepository.ObterPorCampanhaAdAccountAsync(campanhaId, selecao.AdAccountId, cancellationToken);
        if (publicacao is null)
        {
            var campanha = await campanhaRepository.ObterPorIdAsync(campanhaId, cancellationToken)
                ?? throw new KeyNotFoundException("Campanha nao encontrada.");
            publicacao = new MetaAdsPublicacao
            {
                Id = Guid.NewGuid(),
                CampanhaId = campanha.Id,
                MetaAdsContaId = conta.Id,
                AdAccountId = selecao.AdAccountId,
                Status = StatusPublicacaoMetaAds.Preparando,
                UltimaEtapaConcluida = "RegistroCriado",
                DataInicio = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };
            await publicacaoRepository.AdicionarAsync(publicacao, cancellationToken);
            await publicacaoRepository.SalvarAsync(cancellationToken);
        }

        return await ContinueAsync(publicacao, token, config, cancellationToken);
    }

    public async Task<MetaAdsPublicacaoResponse> RetentarAsync(Guid publicacaoId, CancellationToken cancellationToken)
    {
        var publicacao = await publicacaoRepository.ObterPorIdAsync(publicacaoId, cancellationToken)
            ?? throw new KeyNotFoundException("Publicacao Meta nao encontrada.");
        var (conta, token, config, _) = await ContextAsync(cancellationToken);
        if (conta.Id != publicacao.MetaAdsContaId)
        {
            throw new InvalidOperationException("A publicacao Meta pertence a outra conexao. Reconecte a conta correta antes de retentar.");
        }
        return await ContinueAsync(publicacao, token, config, cancellationToken);
    }

    private async Task<MetaAdsPublicacaoResponse> ContinueAsync(MetaAdsPublicacao publicacao, string token, MetaAdsConfiguration config, CancellationToken cancellationToken)
    {
        if (publicacao.Status == StatusPublicacaoMetaAds.Concluida)
        {
            return ToResponse(publicacao, "Campanha ja publicada na Meta em estado pausado.");
        }
        if (publicacao.Status == StatusPublicacaoMetaAds.EstadoIndeterminado)
        {
            return ToResponse(publicacao, "Publicacao em estado indeterminado apos timeout. Reconciliacao manual e necessaria antes de qualquer novo POST.");
        }

        var preview = await previewService.GerarAsync(new MetaAdsPreviewRequest(publicacao.CampanhaId), cancellationToken);
        if (!preview.Preflight.ReadyToPublish && !CanResumePartial(publicacao))
        {
            var blockers = preview.Preflight.Items.Where(x => string.Equals(x.Status, "ERROR", StringComparison.OrdinalIgnoreCase)).Select(x => x.Message);
            throw new InvalidOperationException("Preflight Meta Ads bloqueou a publicacao. " + string.Join(" ", blockers));
        }

        try
        {
            await EnsureReusableResourcesAsync(publicacao, preview, token, config, cancellationToken);

            if (string.IsNullOrWhiteSpace(publicacao.CampaignExternalId))
            {
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.CriandoCampaign, "CriandoCampaign", cancellationToken);
                var created = await graphClient.CreateCampaignAsync(config, token, publicacao.AdAccountId, BuildCampaign(preview), cancellationToken);
                publicacao.CampaignExternalId = created.Id;
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.CampaignCriada, "CampaignCriada", cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(publicacao.AdSetExternalId))
            {
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.CriandoAdSet, "CriandoAdSet", cancellationToken);
                var created = await graphClient.CreateAdSetAsync(config, token, publicacao.AdAccountId, BuildAdSet(preview, publicacao.CampaignExternalId!), cancellationToken);
                publicacao.AdSetExternalId = created.Id;
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.AdSetCriado, "AdSetCriado", cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(publicacao.CreativeExternalId))
            {
                var creativePayload = BuildCreative(preview);
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.CriandoCreative, "CriandoCreative", cancellationToken);
                logger.LogInformation(
                    "Meta creative create step. Edge={MetaEdge} AdAccountId={AdAccountId} CampaignId={CampaignId} AdSetId={AdSetId} PageId={PageId} ImageHash={ImageHash}",
                    "adcreatives",
                    publicacao.AdAccountId,
                    publicacao.CampaignExternalId,
                    publicacao.AdSetExternalId,
                    creativePayload.PageId,
                    creativePayload.ImageHash);
                var created = await graphClient.CreateAdCreativeAsync(config, token, publicacao.AdAccountId, creativePayload, cancellationToken);
                publicacao.CreativeExternalId = created.Id;
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.CreativeCriado, "CreativeCriado", cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(publicacao.AdExternalId))
            {
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.CriandoAd, "CriandoAd", cancellationToken);
                var created = await CreateAdWithDeletedAdSetRecoveryAsync(publicacao, preview, token, config, cancellationToken);
                publicacao.AdExternalId = created.Id;
                publicacao.DataConclusao = DateTime.UtcNow;
                await MarkAsync(publicacao, StatusPublicacaoMetaAds.Concluida, "AdCriado", cancellationToken);
            }

            return ToResponse(publicacao, "Publicado na Meta com sucesso - PAUSADO.");
        }
        catch (MetaAdsGraphApiException ex)
        {
            await FailAsync(publicacao, HasAnyExternalId(publicacao) ? StatusPublicacaoMetaAds.FalhaParcial : StatusPublicacaoMetaAds.Falha, ex.Code, ex.ErrorSubcode, ex.Type, DetailedMetaError(ex), ex.HttpStatusCode?.ToString(), ex.FbTraceId);
            return ToResponse(publicacao, "Falha ao publicar na Meta. Recursos ja criados permaneceram pausados.");
        }
        catch (TaskCanceledException ex)
        {
            await FailAsync(publicacao, StatusPublicacaoMetaAds.EstadoIndeterminado, "timeout_ambiguous", null, "Timeout", "Timeout durante chamada Meta. A criacao pode ter sido concluida remotamente; nao retente antes de reconciliar.", null, null);
            return ToResponse(publicacao, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await FailAndRethrowAsync(publicacao, "invalid_operation", nameof(InvalidOperationException), ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            await FailAndRethrowAsync(publicacao, "http_request_error", nameof(HttpRequestException), ex.Message, ex.StatusCode?.ToString());
            throw;
        }
        catch (Exception ex)
        {
            await FailAndRethrowAsync(publicacao, "unexpected_error", ex.GetType().Name, ex.Message);
            throw;
        }
    }

    private async Task<bool> ReconcileAsync(MetaAdsPublicacao publicacao, string token, MetaAdsConfiguration config, CancellationToken cancellationToken)
    {
        foreach (var (id, etapa) in new[] { (publicacao.CampaignExternalId, "Campaign"), (publicacao.AdSetExternalId, "AdSet"), (publicacao.CreativeExternalId, "Creative"), (publicacao.AdExternalId, "Ad") })
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!await graphClient.ResourceExistsAsync(config, token, id, cancellationToken))
            {
                await FailAsync(publicacao, StatusPublicacaoMetaAds.Inconsistente, "meta_resource_missing", null, null, $"{etapa} persistido nao foi encontrado na Meta.", null, null);
                return false;
            }
        }

        return true;
    }

    private async Task EnsureReusableResourcesAsync(MetaAdsPublicacao publicacao, MetaAdsPreviewResponse preview, string token, MetaAdsConfiguration config, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(publicacao.CampaignExternalId))
        {
            var campaign = await graphClient.GetResourceStatusAsync(config, token, publicacao.CampaignExternalId, cancellationToken);
            if (!IsReusable(campaign))
            {
                await FailAsync(publicacao, StatusPublicacaoMetaAds.Inconsistente, "meta_campaign_not_reusable", null, null, $"Campaign persistida nao pode ser reutilizada. Status remoto: {RemoteStatus(campaign)}.", null, null);
                throw new InvalidOperationException("Campaign Meta persistida nao pode ser reutilizada.");
            }

            LogReconciliation("Campaign", publicacao.CampaignExternalId, campaign, "Reuse");
        }

        if (!string.IsNullOrWhiteSpace(publicacao.AdSetExternalId))
        {
            var adSet = await graphClient.GetResourceStatusAsync(config, token, publicacao.AdSetExternalId, cancellationToken);
            if (!IsReusableForAdCreation(adSet))
            {
                LogReconciliation("AdSet", publicacao.AdSetExternalId, adSet, "Recreate");
                await RecreateAdSetAsync(publicacao, preview, token, config, cancellationToken);
            }
            else
            {
                LogReconciliation("AdSet", publicacao.AdSetExternalId, adSet, "Reuse");
            }
        }

        if (!string.IsNullOrWhiteSpace(publicacao.CreativeExternalId))
        {
            var creative = await graphClient.GetResourceStatusAsync(config, token, publicacao.CreativeExternalId, cancellationToken);
            if (!IsReusable(creative))
            {
                LogReconciliation("Creative", publicacao.CreativeExternalId, creative, "Recreate");
                publicacao.CreativeExternalId = null;
                await publicacaoRepository.SalvarAsync(cancellationToken);
            }
            else
            {
                LogReconciliation("Creative", publicacao.CreativeExternalId, creative, "Reuse");
            }
        }
    }

    private async Task RecreateAdSetAsync(MetaAdsPublicacao publicacao, MetaAdsPreviewResponse preview, string token, MetaAdsConfiguration config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicacao.CampaignExternalId))
        {
            throw new InvalidOperationException("Campaign Meta obrigatoria para recriar Ad Set.");
        }

        publicacao.AdExternalId = null;
        await MarkAsync(publicacao, StatusPublicacaoMetaAds.CriandoAdSet, "CriandoAdSet", cancellationToken);
        var created = await graphClient.CreateAdSetAsync(config, token, publicacao.AdAccountId, BuildAdSet(preview, publicacao.CampaignExternalId), cancellationToken);
        publicacao.AdSetExternalId = created.Id;
        await MarkAsync(publicacao, StatusPublicacaoMetaAds.AdSetCriado, "AdSetCriado", cancellationToken);
        LogReconciliation("AdSet", publicacao.AdSetExternalId, new MetaAdsResourceStatusDto(publicacao.AdSetExternalId, Paused, Paused), "Created");
    }

    private async Task<MetaAdsCreateResult> CreateAdWithDeletedAdSetRecoveryAsync(MetaAdsPublicacao publicacao, MetaAdsPreviewResponse preview, string token, MetaAdsConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            return await graphClient.CreateAdAsync(config, token, publicacao.AdAccountId, BuildAd(preview, publicacao.AdSetExternalId!, publicacao.CreativeExternalId!), cancellationToken);
        }
        catch (MetaAdsGraphApiException ex) when (IsDeletedAdSetError(ex))
        {
            await RecreateAdSetAsync(publicacao, preview, token, config, cancellationToken);
            await MarkAsync(publicacao, StatusPublicacaoMetaAds.CriandoAd, "CriandoAd", cancellationToken);
            return await graphClient.CreateAdAsync(config, token, publicacao.AdAccountId, BuildAd(preview, publicacao.AdSetExternalId!, publicacao.CreativeExternalId!), cancellationToken);
        }
    }

    private static MetaAdsCampaignCreatePayload BuildCampaign(MetaAdsPreviewResponse preview)
    {
        return new MetaAdsCampaignCreatePayload(Name("LeadEngine - " + preview.Campaign.Name), "OUTCOME_TRAFFIC", preview.Campaign.SpecialAdCategories, Paused);
    }

    private static MetaAdsAdSetCreatePayload BuildAdSet(MetaAdsPreviewResponse preview, string campaignId)
    {
        if (preview.AdSet.DailyBudgetMinorUnits is null)
        {
            throw new InvalidOperationException("Orcamento Meta nao convertido.");
        }

        return new MetaAdsAdSetCreatePayload(
            Name("LeadEngine - " + preview.Campaign.Name + " - AdSet"),
            campaignId,
            "LINK_CLICKS",
            "IMPRESSIONS",
            preview.AdSet.DailyBudgetMinorUnits.Value,
            preview.AdSet.BidStrategy,
            BuildTargeting(preview.AdSet.Targeting),
            Paused);
    }

    private static MetaAdsCreativeCreatePayload BuildCreative(MetaAdsPreviewResponse preview)
    {
        if (string.IsNullOrWhiteSpace(preview.Creative.PageId) || string.IsNullOrWhiteSpace(preview.Creative.MetaImageHash))
        {
            throw new InvalidOperationException("Creative Meta sem Page ou image_hash.");
        }

        return new MetaAdsCreativeCreatePayload(
            Name("LeadEngine - " + preview.Campaign.Name + " - Creative"),
            preview.Creative.PageId,
            preview.Creative.InstagramAccountId,
            preview.Creative.MetaImageHash,
            preview.Creative.DestinationUrl,
            preview.Creative.PrimaryText,
            preview.Creative.Headline,
            preview.Creative.Description,
            "LEARN_MORE");
    }

    private static MetaAdsAdCreatePayload BuildAd(MetaAdsPreviewResponse preview, string adSetId, string creativeId)
    {
        return new MetaAdsAdCreatePayload(Name("LeadEngine - " + preview.Campaign.Name + " - Ad"), adSetId, creativeId, Paused);
    }

    private void LogReconciliation(string resourceType, string externalId, MetaAdsResourceStatusDto? status, string action)
    {
        logger.LogInformation(
            "Meta resource reconciliation. ResourceType={ResourceType} ExternalId={ExternalId} RemoteStatus={RemoteStatus} RemoteEffectiveStatus={RemoteEffectiveStatus} Action={Action}",
            resourceType,
            externalId,
            status?.Status,
            status?.EffectiveStatus,
            action);
    }

    private static bool IsReusable(MetaAdsResourceStatusDto? status)
    {
        return status is not null && !IsDeletedOrArchived(status);
    }

    private static bool IsReusableForAdCreation(MetaAdsResourceStatusDto? status)
    {
        return IsReusable(status);
    }

    private static bool IsDeletedOrArchived(MetaAdsResourceStatusDto status)
    {
        return IsTerminal(status.Status) || IsTerminal(status.EffectiveStatus);
    }

    private static bool IsTerminal(string? status)
    {
        return status is not null
            && (status.Equals("DELETED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("ARCHIVED", StringComparison.OrdinalIgnoreCase));
    }

    private static string RemoteStatus(MetaAdsResourceStatusDto? status)
    {
        return status is null ? "NOT_FOUND" : status.EffectiveStatus ?? status.Status ?? "UNKNOWN";
    }

    private static bool IsDeletedAdSetError(MetaAdsGraphApiException ex)
    {
        return ex.Code == "100" && ex.ErrorSubcode == "1487861";
    }

    private static MetaAdsTargetingCreatePayload BuildTargeting(MetaAdsTargetingPreview targeting)
    {
        var location = targeting.Location ?? throw new InvalidOperationException("Targeting Meta sem localizacao resolvida.");
        var countries = new List<string>();
        var regions = new List<MetaAdsTargetingLocationPayload>();
        var cities = new List<MetaAdsTargetingLocationPayload>();
        if (string.Equals(location.Type, "city", StringComparison.OrdinalIgnoreCase))
        {
            cities.Add(new MetaAdsTargetingLocationPayload(location.Key));
        }
        else if (string.Equals(location.Type, "region", StringComparison.OrdinalIgnoreCase))
        {
            regions.Add(new MetaAdsTargetingLocationPayload(location.Key));
        }
        else
        {
            countries.Add(location.CountryCode ?? targeting.Countries.FirstOrDefault() ?? "BR");
        }

        return new MetaAdsTargetingCreatePayload(countries, regions, cities, targeting.AgeMin, targeting.AgeMax);
    }

    private async Task<(MetaAdsConta Conta, string Token, MetaAdsConfiguration Config, MetaAdsAtivoSelecionado Selecao)> ContextAsync(CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterAtivaAsync(cancellationToken)
            ?? throw new InvalidOperationException("Meta Ads nao conectado.");
        if (string.IsNullOrWhiteSpace(conta.AccessTokenProtegido))
        {
            throw new InvalidOperationException("Reconecte Meta Ads antes de publicar.");
        }
        if (conta.AccessTokenExpiraEm is not null && conta.AccessTokenExpiraEm <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Token Meta expirado. Reconecte Meta Ads.");
        }

        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken)
            ?? throw new InvalidOperationException("Selecione os ativos Meta antes de publicar.");
        return (conta, protector.Unprotect(conta.AccessTokenProtegido), await Config(cancellationToken), selecao);
    }

    private async Task<MetaAdsConfiguration> Config(CancellationToken cancellationToken)
    {
        return new MetaAdsConfiguration(
            await Value("AppId", cancellationToken),
            await Value("AppSecret", cancellationToken),
            await Value("RedirectUri", cancellationToken) ?? string.Empty,
            await Value("AuthEndpoint", cancellationToken) ?? string.Empty,
            await Value("TokenEndpoint", cancellationToken) ?? string.Empty,
            await Value("UserInfoEndpoint", cancellationToken) ?? string.Empty,
            await Value("GraphApiBaseUrl", cancellationToken) ?? string.Empty,
            await Value("GraphApiVersion", cancellationToken) ?? string.Empty,
            await Value("Scopes", cancellationToken) ?? string.Empty);
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.MetaAds, key, cancellationToken)).Value;
    }

    private async Task MarkAsync(MetaAdsPublicacao publicacao, StatusPublicacaoMetaAds status, string etapa, CancellationToken cancellationToken)
    {
        publicacao.Status = status;
        publicacao.UltimaEtapaConcluida = etapa;
        publicacao.DataAtualizacao = DateTime.UtcNow;
        publicacao.UltimoErroCodigo = null;
        publicacao.UltimoErroSubcodigo = null;
        publicacao.UltimoErroMensagem = null;
        publicacao.UltimoErroTipo = null;
        publicacao.UltimoErroHttpStatus = null;
        publicacao.FbTraceId = null;
        await publicacaoRepository.SalvarAsync(cancellationToken);
    }

    private async Task FailAsync(MetaAdsPublicacao publicacao, StatusPublicacaoMetaAds status, string? code, string? subcode, string? type, string? message, string? httpStatus, string? traceId)
    {
        publicacao.Status = status;
        publicacao.UltimaEtapaConcluida = FailureStep(publicacao);
        publicacao.DataAtualizacao = DateTime.UtcNow;
        publicacao.UltimoErroCodigo = code;
        publicacao.UltimoErroSubcodigo = subcode;
        publicacao.UltimoErroTipo = type;
        publicacao.UltimoErroMensagem = message is { Length: > 500 } ? message[..500] : message;
        publicacao.UltimoErroHttpStatus = httpStatus;
        publicacao.FbTraceId = traceId;
        await PersistFailureAsync();
    }

    private async Task FailAndRethrowAsync(MetaAdsPublicacao publicacao, string code, string type, string? message, string? httpStatus = null)
    {
        if (!string.IsNullOrWhiteSpace(publicacao.UltimoErroCodigo))
        {
            logger.LogError(
                "Meta publishing failed after controlled persistence. Stage={Stage} Status={Status} ErrorCode={ErrorCode} ErrorType={ErrorType} Message={Message}",
                FailureStep(publicacao),
                publicacao.Status,
                publicacao.UltimoErroCodigo,
                publicacao.UltimoErroTipo,
                message);
            return;
        }

        var status = HasAnyExternalId(publicacao) ? StatusPublicacaoMetaAds.FalhaParcial : StatusPublicacaoMetaAds.Falha;
        logger.LogError(
            "Meta publishing failed. Stage={Stage} Status={Status} ErrorCode={ErrorCode} ErrorType={ErrorType} HttpStatus={HttpStatus} Message={Message}",
            FailureStep(publicacao),
            status,
            code,
            type,
            httpStatus,
            message);
        await FailAsync(publicacao, status, code, null, type, message, httpStatus, null);
    }

    private static string FailureStep(MetaAdsPublicacao publicacao)
    {
        return publicacao.UltimaEtapaConcluida == "AdSetCriado" && string.IsNullOrWhiteSpace(publicacao.CreativeExternalId)
            ? "CriandoCreative"
            : publicacao.UltimaEtapaConcluida;
    }

    private async Task PersistFailureAsync()
    {
        using var persistenceCts = new CancellationTokenSource(FailurePersistenceTimeout);
        await publicacaoRepository.SalvarAsync(persistenceCts.Token);
    }

    private static MetaAdsPublicacaoResponse ToResponse(MetaAdsPublicacao publicacao, string? mensagem = null)
    {
        return new MetaAdsPublicacaoResponse(
            publicacao.Id,
            publicacao.CampanhaId,
            publicacao.Status.ToString(),
            publicacao.UltimaEtapaConcluida,
            publicacao.CampaignExternalId,
            publicacao.AdSetExternalId,
            publicacao.CreativeExternalId,
            publicacao.AdExternalId,
            publicacao.DataInicio,
            publicacao.DataConclusao,
            publicacao.DataAtualizacao,
            publicacao.UltimoErroCodigo,
            publicacao.UltimoErroSubcodigo,
            publicacao.UltimoErroMensagem,
            publicacao.FbTraceId,
            publicacao.Status is StatusPublicacaoMetaAds.FalhaParcial or StatusPublicacaoMetaAds.Falha,
            mensagem ?? StatusMessage(publicacao));
    }

    private static string StatusMessage(MetaAdsPublicacao publicacao)
    {
        return publicacao.Status == StatusPublicacaoMetaAds.Concluida
            ? "Publicado na Meta com sucesso - PAUSADO."
            : publicacao.UltimoErroMensagem ?? "Publicacao Meta em andamento ou pendente.";
    }

    private static bool HasAnyExternalId(MetaAdsPublicacao publicacao)
    {
        return !string.IsNullOrWhiteSpace(publicacao.CampaignExternalId)
            || !string.IsNullOrWhiteSpace(publicacao.AdSetExternalId)
            || !string.IsNullOrWhiteSpace(publicacao.CreativeExternalId)
            || !string.IsNullOrWhiteSpace(publicacao.AdExternalId);
    }

    private static bool CanResumePartial(MetaAdsPublicacao publicacao)
    {
        return publicacao.Status == StatusPublicacaoMetaAds.FalhaParcial
            && (!string.IsNullOrWhiteSpace(publicacao.CampaignExternalId)
                || !string.IsNullOrWhiteSpace(publicacao.AdSetExternalId)
                || !string.IsNullOrWhiteSpace(publicacao.CreativeExternalId)
                || !string.IsNullOrWhiteSpace(publicacao.AdExternalId));
    }

    private static string DetailedMetaError(MetaAdsGraphApiException ex)
    {
        var parts = new List<string>();
        Add(parts, "HTTP", ex.HttpStatusCode?.ToString());
        Add(parts, "message", ex.MetaMessage ?? ex.Message);
        Add(parts, "type", ex.Type);
        Add(parts, "code", ex.Code);
        Add(parts, "error_subcode", ex.ErrorSubcode);
        Add(parts, "error_user_title", ex.ErrorUserTitle);
        Add(parts, "error_user_msg", ex.ErrorUserMessage);
        Add(parts, "fbtrace_id", ex.FbTraceId);
        Add(parts, "blame_field", ex.BlameField);
        Add(parts, "blame_field_specs", ex.BlameFieldSpecs);
        Add(parts, "error_data", ex.ErrorData);
        return string.Join("; ", parts);
    }

    private static void Add(List<string> parts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{name}={value}");
        }
    }

    private static string Name(string value)
    {
        var clean = value.Trim();
        return clean.Length <= 180 ? clean : clean[..180].Trim();
    }
}
