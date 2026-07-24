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
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddScoped<IGoogleAdsContaRepository, GoogleAdsContaRepository>();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IConfigurationResolver, ConfigurationResolver>();
        services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
        services.AddScoped<IGoogleAdsOAuthClient, GoogleAdsOAuthClient>();
        services.AddScoped<IGoogleAdsTokenService, GoogleAdsTokenService>();
        services.AddScoped<IGoogleAdsConnectionService, GoogleAdsConnectionService>();
        services.AddScoped<IGoogleAdsPlanoPublicacaoRepository, GoogleAdsPlanoPublicacaoRepository>();
        services.AddScoped<IGoogleAdsCampaignMappingService, GoogleAdsCampaignMappingService>();
        services.AddScoped<IGoogleAdsValidationService, GoogleAdsValidationService>();
        services.AddScoped<IGoogleAdsCopyAdjustmentService, OpenRouterGoogleAdsCopyAdjustmentService>();
        services.AddScoped<IGoogleAdsPreviewService, GoogleAdsPreviewService>();
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
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddHttpClient("googleads", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
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
