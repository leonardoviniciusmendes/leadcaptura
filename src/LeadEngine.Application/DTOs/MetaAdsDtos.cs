namespace LeadEngine.Application.DTOs;

public sealed record MetaAdsAuthUrlResponse(string Url, string State);

public sealed record MetaAdsOAuthCallbackRequest(string Code, string? State);

public sealed record MetaAdsOAuthCallbackResponse(
    bool Sucesso,
    bool Conectado,
    string Mensagem,
    MetaAdsStatusResponse Status);

public sealed record MetaAdsStatusResponse(
    bool Configurado,
    bool Conectado,
    bool ContaSelecionada,
    string Status,
    Guid? ContaId = null,
    string? MetaUserId = null,
    string? Nome = null,
    DateTime? DataConexao = null,
    DateTime? AccessTokenExpiraEm = null);

public sealed record MetaAdsTokenResult(
    string AccessToken,
    string? TokenType,
    int? ExpiresIn);

public sealed record MetaAdsUserInfo(string Id, string? Name);

public sealed record MetaAdsAssetListResponse<T>(
    bool Sucesso,
    IReadOnlyList<T> Itens,
    string? Mensagem = null,
    bool PermissaoNecessaria = false);

public sealed record MetaAdsBusinessResponse(string Id, string Nome);

public sealed record MetaAdsAdAccountResponse(
    string Id,
    string? AccountId,
    string Nome,
    string? Status,
    string? Moeda);

public sealed record MetaAdsInstagramAccountResponse(string Id, string? Nome, string? Username);

public sealed record MetaAdsPageResponse(
    string Id,
    string Nome,
    MetaAdsInstagramAccountResponse? Instagram);

public sealed record MetaAdsPixelResponse(string Id, string Nome);

public sealed record MetaAdsAssetSelectionResponse(
    Guid? Id,
    Guid? MetaAdsContaId,
    string? BusinessId,
    string? BusinessNome,
    string? AdAccountId,
    string? AdAccountNome,
    string? PageId,
    string? PageNome,
    string? InstagramAccountId,
    string? InstagramNome,
    string? PixelId,
    string? PixelNome,
    DateTime? DataAtualizacao);

public sealed record MetaAdsAssetSelectionRequest(
    string? BusinessId,
    string? AdAccountId,
    string? PageId,
    string? PixelId);

public sealed record MetaAdsPreviewRequest(
    Guid CampanhaId,
    string? SpecialAdCategory = "NONE",
    int? IdadeMinima = null,
    int? IdadeMaxima = null,
    string? LocationKey = null);

public sealed record MetaAdsPreviewResponse(
    Guid CampanhaId,
    MetaAdsPreviewAssets Assets,
    MetaAdsCampaignPreview Campaign,
    MetaAdsAdSetPreview AdSet,
    MetaAdsCreativePreview Creative,
    MetaAdsAdPreview Ad,
    MetaAdsPreflight Preflight);

public sealed record MetaAdsPreviewAssets(
    string? BusinessId,
    string? BusinessNome,
    string? AdAccountId,
    string? AdAccountNome,
    string? PageId,
    string? PageNome,
    string? InstagramAccountId,
    string? InstagramNome,
    string? PixelId,
    string? PixelNome);

public sealed record MetaAdsCampaignPreview(
    string Name,
    string Objective,
    string Status,
    string SpecialAdCategory,
    IReadOnlyList<string> SpecialAdCategories);

public sealed record MetaAdsAdSetPreview(
    string Name,
    string CampaignObjective,
    decimal DailyBudget,
    long? DailyBudgetMinorUnits,
    string? Currency,
    string BillingEvent,
    string OptimizationGoal,
    string BidStrategy,
    MetaAdsTargetingPreview Targeting,
    DateTime? StartTime,
    DateTime? EndTime,
    string? PixelId);

public sealed record MetaAdsTargetingPreview(
    IReadOnlyList<string> Countries,
    MetaAdsLocationResponse? Location,
    string? RegionText,
    string? CityText,
    int AgeMin,
    int AgeMax);

public sealed record MetaAdsCreativePreview(
    string? PageId,
    string? InstagramAccountId,
    string PrimaryText,
    string Headline,
    string Description,
    string DestinationUrl,
    string CallToAction,
    string? ImageUrl,
    string? MediaReference,
    string? MetaImageHash,
    bool MediaUploaded);

public sealed record MetaAdsAdPreview(string Name, string Status);

public sealed record MetaAdsPreflight(
    bool ReadyToPublish,
    IReadOnlyList<MetaAdsPreflightItem> Items);

public sealed record MetaAdsPreflightItem(
    string Code,
    string Status,
    string Message);

public sealed record MetaAdsPermissionStatusResponse(
    IReadOnlyList<MetaAdsPermissionResponse> Permissions)
{
    public IReadOnlyList<string> Granted => Permissions.Where(x => x.Status == "Granted").Select(x => x.Permission).ToArray();
    public IReadOnlyList<string> Declined => Permissions.Where(x => x.Status == "Declined").Select(x => x.Permission).ToArray();
}

public sealed record MetaAdsPermissionResponse(string Permission, string Status);

public sealed record MetaAdsLocationResponse(
    string Key,
    string Name,
    string Type,
    string? CountryCode,
    string? CountryName,
    string? Region,
    string? RegionId,
    bool SupportsRegion,
    bool SupportsCity);

public sealed record MetaAdsLocationSearchResponse(
    bool Sucesso,
    IReadOnlyList<MetaAdsLocationResponse> Itens,
    string? Mensagem = null,
    bool PermissaoNecessaria = false);

public sealed record MetaAdsTargetingSelectionRequest(
    Guid CampanhaId,
    string LocationKey,
    int? IdadeMinima = null,
    int? IdadeMaxima = null);

public sealed record MetaAdsUploadImageResponse(
    bool Sucesso,
    Guid? ImagemId,
    string NomeArquivo,
    string ContentType,
    long? TamanhoBytes,
    string ContentHash,
    string? MetaImageHash,
    bool Reutilizado,
    DateTime? DataUpload,
    string Mensagem);

public sealed record MetaAdsPublicacaoResponse(
    Guid Id,
    Guid CampanhaId,
    string Status,
    string UltimaEtapaConcluida,
    string? CampaignExternalId,
    string? AdSetExternalId,
    string? CreativeExternalId,
    string? AdExternalId,
    DateTime DataInicio,
    DateTime? DataConclusao,
    DateTime? DataAtualizacao,
    string? UltimoErroCodigo,
    string? UltimoErroSubcodigo,
    string? UltimoErroMensagem,
    string? FbTraceId,
    bool PodeTentarNovamente,
    string Mensagem);

public sealed record MetaAdsPublicationStatusResponse(
    bool Existe,
    MetaAdsPublicacaoResponse? Publicacao);

public sealed record MetaAdsCreateResult(string Id);

public sealed record MetaAdAccountDto(
    string Id,
    string? Name,
    string? AccountStatus,
    string? Currency,
    string? TimezoneName);

public sealed record MetaCampaignDto(
    string Id,
    string? Name,
    string? Status,
    string? EffectiveStatus,
    string? BidStrategy = null);

public sealed record MetaAdSetDto(
    string Id,
    string? Name,
    string? Status,
    string? EffectiveStatus,
    string? CampaignId,
    string? DailyBudget,
    string? LifetimeBudget);

public sealed record MetaAdDto(
    string Id,
    string? Name,
    string? Status,
    string? EffectiveStatus,
    string? AdSetId,
    string? CampaignId);

public sealed record MetaCreativeDto(
    string Id,
    string? Name,
    string? Status,
    string? ObjectStoryId,
    string? ObjectStorySpec);

public sealed record CreateMetaCampaignRequest(
    string Name,
    string? Objective = null,
    IReadOnlyList<string>? SpecialAdCategories = null,
    string? Status = null);

public sealed record CreateMetaCampaignResponse(string Id);

public sealed record DeleteMetaCampaignResponse(bool Success);

public sealed record CreateMetaAdSetRequest(
    string CampaignId,
    string Name,
    long DailyBudget,
    string? BillingEvent,
    string? OptimizationGoal,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    MetaTargetingRequest? Targeting);

public sealed record MetaTargetingRequest(
    IReadOnlyList<string>? Countries,
    IReadOnlyList<MetaLocationKeyRequest>? Regions,
    IReadOnlyList<MetaLocationKeyRequest>? Cities,
    int? AgeMin,
    int? AgeMax,
    IReadOnlyList<int>? Genders);

public sealed record MetaLocationKeyRequest(string Key);

public sealed record CreateMetaAdSetResponse(string Id);

public sealed record DeleteMetaAdSetResponse(bool Success);

public sealed record CreateMetaCreativeRequest(
    string Name,
    string PageId,
    string ImageHash,
    string Message,
    string LinkUrl,
    string Headline,
    string? Description = null,
    string? CallToActionType = null);

public sealed record CreateMetaCreativeResponse(string Id);

public sealed record DeleteMetaCreativeResponse(bool Success);

public sealed record CreateMetaAdRequest(
    string Name,
    string AdSetId,
    string CreativeId);

public sealed record CreateMetaAdResponse(string Id);

public sealed record DeleteMetaAdResponse(bool Success);

public sealed record MetaAdsCampaignCreatePayload(
    string Name,
    string Objective,
    IReadOnlyList<string> SpecialAdCategories,
    string Status,
    string? BidStrategy = null);

public sealed record MetaAdsAdSetCreatePayload(
    string Name,
    string CampaignId,
    string OptimizationGoal,
    string BillingEvent,
    long DailyBudget,
    string? BidStrategy,
    MetaAdsTargetingCreatePayload Targeting,
    string Status,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null);

public sealed record MetaAdsTargetingCreatePayload(
    IReadOnlyList<string> Countries,
    IReadOnlyList<MetaAdsTargetingLocationPayload> Regions,
    IReadOnlyList<MetaAdsTargetingLocationPayload> Cities,
    int? AgeMin,
    int? AgeMax,
    IReadOnlyList<int>? Genders = null,
    int? AdvantageAudience = null);

public sealed record MetaAdsTargetingLocationPayload(string Key);

public sealed record MetaAdsCreativeCreatePayload(
    string Name,
    string PageId,
    string? InstagramActorId,
    string ImageHash,
    string Link,
    string Message,
    string Headline,
    string Description,
    string CallToAction);

public sealed record MetaAdsDiagnosticCreativeCreatePayload(
    string Name,
    string PageId,
    string ImageHash,
    string Link,
    string Message,
    string Headline,
    string? Description,
    string? CallToAction);

public sealed record MetaAdsAdCreatePayload(
    string Name,
    string AdSetId,
    string CreativeId,
    string Status);
