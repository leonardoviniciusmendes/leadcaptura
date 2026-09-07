using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Infrastructure.Configuration;
using LeadEngine.Infrastructure.CampaignGeneration;
using LeadEngine.Infrastructure.GoogleAds;
using LeadEngine.Infrastructure.Integrations;
using LeadEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeadEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Port=3306;Database=leadengine;User=leadengine;Password=leadengine;";

        services.AddDbContext<LeadEngineDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ICampanhaRepository, CampanhaRepository>();
        services.AddScoped<ICreativeAssetRepository, CreativeAssetRepository>();
        services.AddScoped<ICreativeQualityOverrideRepository, CreativeQualityOverrideRepository>();
        services.AddScoped<ISegmentRepository, SegmentRepository>();
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddScoped<IGoogleAdsContaRepository, GoogleAdsContaRepository>();
        services.AddScoped<IGoogleAdsOAuthStateRepository, GoogleAdsOAuthStateRepository>();
        services.AddScoped<IMetaAdsContaRepository, MetaAdsContaRepository>();
        services.AddScoped<IMetaAdsOAuthStateRepository, MetaAdsOAuthStateRepository>();
        services.AddScoped<IMetaAdsAtivoSelecionadoRepository, MetaAdsAtivoSelecionadoRepository>();
        services.AddScoped<IMetaAdsImagemRepository, MetaAdsImagemRepository>();
        services.AddScoped<IMetaAdsVideoRepository, MetaAdsVideoRepository>();
        services.AddScoped<IMetaAdsPreparacaoPublicacaoRepository, MetaAdsPreparacaoPublicacaoRepository>();
        services.AddScoped<IMetaAdsPublicacaoRepository, MetaAdsPublicacaoRepository>();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IConfigurationResolver, ConfigurationResolver>();
        services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
        services.AddScoped<IMetaAdsOAuthClient, MetaAdsOAuthClient>();
        services.AddScoped<IMetaAdsConnectionService, MetaAdsConnectionService>();
        services.AddScoped<IMetaAdsGraphClient, MetaAdsGraphClient>();
        services.AddScoped<IMetaAdsAssetService, MetaAdsAssetService>();
        services.AddScoped<IMetaAdsPreviewService, MetaAdsPreviewService>();
        services.AddScoped<IMetaAdsPublicationPreparationService, MetaAdsPublicationPreparationService>();
        services.AddScoped<IMetaAdsPublishingService, MetaAdsPublishingService>();
        services.AddScoped<IMetaAdsDiagnosticsService, MetaAdsDiagnosticsService>();
        services.AddScoped<IGoogleAdsOAuthClient, GoogleAdsOAuthClient>();
        services.AddScoped<IGoogleAdsTokenService, GoogleAdsTokenService>();
        services.AddScoped<IGoogleAdsConnectionService, GoogleAdsConnectionService>();
        services.AddScoped<IGoogleAdsDiagnosticsService, GoogleAdsDiagnosticsService>();
        services.AddScoped<IGoogleAdsPlanoPublicacaoRepository, GoogleAdsPlanoPublicacaoRepository>();
        services.AddScoped<IGoogleAdsPublicationRepository, GoogleAdsPublicationRepository>();
        services.AddScoped<IGoogleAdsMetricsRepository, GoogleAdsMetricsRepository>();
        services.AddScoped<IGoogleAdsSynchronizationRepository, GoogleAdsSynchronizationRepository>();
        services.AddScoped<IGoogleAdsAnalysisRepository, GoogleAdsAnalysisRepository>();
        services.AddScoped<CampaignPublicUrlBuilder>();
        services.AddScoped<CreativeAssetService>();
        services.AddScoped<CreativeQualityGateService>();
        services.AddScoped<IVideoProcessingService, FfmpegVideoProcessingService>();
        services.AddScoped<FakeCreativeAssetAnalysisProvider>();
        services.AddScoped<OpenRouterCreativeAssetAnalysisProvider>();
        services.AddScoped<ICreativeAssetAnalysisProvider, ConfiguredCreativeAssetAnalysisProvider>();
        services.AddScoped<IGoogleAdsCampaignMappingService, GoogleAdsCampaignMappingService>();
        services.AddScoped<IGoogleAdsValidationService, GoogleAdsValidationService>();
        services.AddScoped<IGoogleAdsCopyAdjustmentService, OpenRouterGoogleAdsCopyAdjustmentService>();
        services.AddScoped<IGoogleAdsPreviewService, GoogleAdsPreviewService>();
        services.AddScoped<IGoogleAdsGeoTargetResolver, GoogleAdsGeoTargetResolver>();
        services.AddScoped<IGoogleAdsLanguageResolver, GoogleAdsLanguageResolver>();
        services.AddScoped<IGoogleAdsOperationBuilder, GoogleAdsOperationBuilder>();
        services.AddScoped<IGoogleAdsErrorTranslator, GoogleAdsErrorTranslator>();
        services.AddSingleton<GoogleAdsExceptionFormatter>();
        services.AddScoped<GoogleAdsTypedOperationFactory>();
        services.AddScoped<GoogleAdsRestMutateTransport>();
        services.AddScoped<IGoogleAdsMutationClient, GoogleAdsMutationClient>();
        services.AddScoped<IGoogleAdsResourceQueryClient, GoogleAdsResourceQueryClient>();
        services.AddScoped<GoogleAdsGaqlClient>();
        services.AddScoped<IGoogleAdsDiagnosticsQueryClient, GoogleAdsDiagnosticsQueryClient>();
        services.AddScoped<IGoogleAdsMetricsQueryClient, GoogleAdsMetricsQueryClient>();
        services.AddScoped<IGoogleAdsSynchronizationQueryClient, GoogleAdsSynchronizationQueryClient>();
        services.AddScoped<IGoogleAdsRemoteValidationService, GoogleAdsRemoteValidationService>();
        services.AddScoped<IGoogleAdsPublishingService, GoogleAdsPublishingService>();
        services.AddScoped<ILeadAttributionService, LeadAttributionService>();
        services.AddScoped<IGoogleAdsMetricsService, GoogleAdsMetricsService>();
        services.AddScoped<IGoogleAdsSynchronizationService, GoogleAdsSynchronizationService>();
        services.AddScoped<IGoogleAdsOptimizationService, OpenRouterGoogleAdsOptimizationService>();
        services.AddHostedService<GoogleAdsMetricsSyncWorker>();
        services.AddMemoryCache();
        services.AddDataProtection();
        services.AddScoped<CampaignPromptBuilder>();
        services.AddScoped<CampaignGenerationResponseParser>();
        services.AddScoped<CampaignSectionPromptBuilder>();
        services.AddScoped<CampaignSectionResponseParser>();
        services.AddScoped<FakeCampaignGenerationService>();
        services.AddScoped<OpenRouterCampaignGenerationService>();
        services.AddScoped<ICampaignSectionGenerationService, OpenRouterCampaignSectionGenerationService>();
        services.AddScoped<ICampaignGenerationService, ConfiguredCampaignGenerationService>();
        services.AddScoped<ICampaignPublicationService, CampaignPublicationService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IWhatsAppUrlBuilder, WhatsAppUrlBuilder>();
        services.Configure<CampaignGenerationOptions>(configuration.GetSection("CampaignGeneration"));
        services.Configure<LeadCaptureOptions>(configuration.GetSection("LeadCapture"));
        services.Configure<WhatsAppOptions>(options =>
        {
            configuration.GetSection("WhatsApp").Bind(options);
            var numero = Environment.GetEnvironmentVariable("WHATSAPP_NUMERO");
            var mensagem = Environment.GetEnvironmentVariable("WHATSAPP_MENSAGEM_PADRAO");
            if (!string.IsNullOrWhiteSpace(numero))
            {
                options.Numero = numero;
            }

            if (!string.IsNullOrWhiteSpace(mensagem))
            {
                options.MensagemPadrao = mensagem;
            }
        });
        services.Configure<CreativeAssetOptions>(configuration.GetSection("CreativeAssets"));
        services.Configure<VideoProcessingOptions>(configuration.GetSection("VideoProcessing"));
        services.Configure<CreativeAnalysisOptions>(options =>
        {
            configuration.GetSection("CreativeAnalysis").Bind(options);
            var provider = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_PROVIDER");
            var model = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_MODEL");
            var costTier = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_COST_TIER");
            var fallback = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_FALLBACK_TO_FAKE");
            if (!string.IsNullOrWhiteSpace(provider))
            {
                options.Provider = provider;
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                options.Model = model;
            }

            if (!string.IsNullOrWhiteSpace(costTier))
            {
                options.CostTier = costTier;
            }

            if (bool.TryParse(fallback, out var fallbackToFake))
            {
                options.FallbackToFake = fallbackToFake;
            }

            if (string.IsNullOrWhiteSpace(options.Model))
            {
                options.Model = CreativeAnalysisOptions.DefaultModel;
            }

            if (string.IsNullOrWhiteSpace(options.CostTier))
            {
                options.CostTier = CreativeAnalysisOptions.DefaultCostTier;
            }
        });
        services.Configure<OpenRouterOptions>(options =>
        {
            configuration.GetSection("OpenRouter").Bind(options);
            var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            var model = Environment.GetEnvironmentVariable("OPENROUTER_MODEL");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                options.ApiKey = apiKey;
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                options.Model = model;
            }
        });
        services.AddHttpClient("openrouter", (provider, client) =>
        {
            var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenRouterOptions>>().Value;
            client.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(300);
        });
        services.AddHttpClient("googleads", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient("metaads", client =>
        {
            var timeoutText = Environment.GetEnvironmentVariable("META_ADS_API_TIMEOUT_SECONDS")
                ?? configuration["MetaAds:ApiTimeoutSeconds"];
            client.Timeout = TimeSpan.FromSeconds(int.TryParse(timeoutText, out var timeout) && timeout > 0 ? timeout : 30);
        });
        services.Configure<IntegracaoLeadsOptions>(configuration.GetSection("IntegracaoLeads"));
        services.AddHttpClient<IIntegracaoLeadService, IntegracaoLeadService>((provider, client) =>
        {
            var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegracaoLeadsOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                client.BaseAddress = new Uri(config.BaseUrl);
            }

            client.Timeout = TimeSpan.FromSeconds(20);
        });

        return services;
    }
}
