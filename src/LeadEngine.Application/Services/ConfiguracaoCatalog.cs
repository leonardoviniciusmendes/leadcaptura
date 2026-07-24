using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed record ConfigDefinition(CategoriaConfiguracao Categoria, string Key, TipoConfiguracao Tipo, bool Sensivel, string? DefaultValue, string EnvName, string AppSettingsPath, string Descricao);

public static class ConfiguracaoCatalog
{
    public static readonly IReadOnlyList<ConfigDefinition> Items =
    [
        new(CategoriaConfiguracao.OpenRouter, "ApiKey", TipoConfiguracao.Segredo, true, null, "OPENROUTER_API_KEY", "OpenRouter:ApiKey", "Chave da API OpenRouter."),
        new(CategoriaConfiguracao.OpenRouter, "Model", TipoConfiguracao.Texto, false, "", "OPENROUTER_MODEL", "OpenRouter:Model", "Modelo de IA."),
        new(CategoriaConfiguracao.OpenRouter, "BaseUrl", TipoConfiguracao.Url, false, "https://openrouter.ai/api/v1", "OPENROUTER_BASE_URL", "OpenRouter:BaseUrl", "URL base OpenRouter."),
        new(CategoriaConfiguracao.OpenRouter, "TimeoutSeconds", TipoConfiguracao.Numero, false, "60", "OPENROUTER_TIMEOUT_SECONDS", "OpenRouter:TimeoutSeconds", "Timeout em segundos."),
        new(CategoriaConfiguracao.OpenRouter, "MaxRetries", TipoConfiguracao.Numero, false, "2", "OPENROUTER_MAX_RETRIES", "OpenRouter:MaxRetries", "Tentativas de retry."),
        new(CategoriaConfiguracao.OpenRouter, "Temperature", TipoConfiguracao.Decimal, false, "0.3", "OPENROUTER_TEMPERATURE", "OpenRouter:Temperature", "Temperatura do modelo."),
        new(CategoriaConfiguracao.CampaignGeneration, "Provider", TipoConfiguracao.Texto, false, "Fake", "CAMPAIGN_GENERATION_PROVIDER", "CampaignGeneration:Provider", "Provider de geracao."),
        new(CategoriaConfiguracao.CampaignGeneration, "FallbackToFake", TipoConfiguracao.Booleano, false, "false", "CAMPAIGN_GENERATION_FALLBACK_TO_FAKE", "CampaignGeneration:FallbackToFake", "Fallback para fake."),
        new(CategoriaConfiguracao.WhatsApp, "Numero", TipoConfiguracao.Texto, false, "", "WHATSAPP_NUMERO", "WhatsApp:Numero", "Numero internacional."),
        new(CategoriaConfiguracao.WhatsApp, "MensagemPadrao", TipoConfiguracao.Texto, false, "Gostaria de receber uma cotacao.", "WHATSAPP_MENSAGEM_PADRAO", "WhatsApp:MensagemPadrao", "Mensagem final padrao."),
        new(CategoriaConfiguracao.LeadCapture, "ConsentVersion", TipoConfiguracao.Texto, false, "1.0", "LEAD_CAPTURE_CONSENT_VERSION", "LeadCapture:ConsentVersion", "Versao do consentimento."),
        new(CategoriaConfiguracao.LeadCapture, "MinimumFormSeconds", TipoConfiguracao.Numero, false, "2", "LEAD_CAPTURE_MINIMUM_FORM_SECONDS", "LeadCapture:MinimumFormSeconds", "Tempo minimo do formulario."),
        new(CategoriaConfiguracao.LeadCapture, "MaxLeadsPerIpPerHour", TipoConfiguracao.Numero, false, "10", "LEAD_CAPTURE_MAX_LEADS_PER_IP_PER_HOUR", "LeadCapture:MaxLeadsPerIpPerHour", "Rate limit por IP."),
        new(CategoriaConfiguracao.LeadCapture, "DuplicateWindowHours", TipoConfiguracao.Numero, false, "24", "LEAD_CAPTURE_DUPLICATE_WINDOW_HOURS", "LeadCapture:DuplicateWindowHours", "Janela de duplicidade."),
        new(CategoriaConfiguracao.ExternalLeadApi, "Enabled", TipoConfiguracao.Booleano, false, "false", "EXTERNAL_LEAD_API_ENABLED", "ExternalLeadApi:Enabled", "Ativa API externa."),
        new(CategoriaConfiguracao.ExternalLeadApi, "BaseUrl", TipoConfiguracao.Url, false, "", "EXTERNAL_LEAD_API_BASE_URL", "ExternalLeadApi:BaseUrl", "URL base externa."),
        new(CategoriaConfiguracao.ExternalLeadApi, "ApiKey", TipoConfiguracao.Segredo, true, null, "EXTERNAL_LEAD_API_API_KEY", "ExternalLeadApi:ApiKey", "Chave externa."),
        new(CategoriaConfiguracao.ExternalLeadApi, "TimeoutSeconds", TipoConfiguracao.Numero, false, "20", "EXTERNAL_LEAD_API_TIMEOUT_SECONDS", "ExternalLeadApi:TimeoutSeconds", "Timeout externo."),
        new(CategoriaConfiguracao.ExternalLeadApi, "MaxRetries", TipoConfiguracao.Numero, false, "2", "EXTERNAL_LEAD_API_MAX_RETRIES", "ExternalLeadApi:MaxRetries", "Retries externo."),
        new(CategoriaConfiguracao.Application, "PublicBaseUrl", TipoConfiguracao.Url, false, "", "APPLICATION_PUBLIC_BASE_URL", "Application:PublicBaseUrl", "URL publica da aplicacao."),
        new(CategoriaConfiguracao.Landing, "DefaultFooterText", TipoConfiguracao.Texto, false, "Valores, redes, carencias e coberturas dependem do plano.", "LANDING_DEFAULT_FOOTER_TEXT", "Landing:DefaultFooterText", "Rodape padrao."),
        new(CategoriaConfiguracao.Landing, "PrivacyPolicyUrl", TipoConfiguracao.Url, false, "", "LANDING_PRIVACY_POLICY_URL", "Landing:PrivacyPolicyUrl", "URL de privacidade."),
        new(CategoriaConfiguracao.GoogleAds, "ClientId", TipoConfiguracao.Texto, false, "", "GOOGLE_ADS_CLIENT_ID", "GoogleAds:ClientId", "Client ID OAuth do Google."),
        new(CategoriaConfiguracao.GoogleAds, "ClientSecret", TipoConfiguracao.Segredo, true, null, "GOOGLE_ADS_CLIENT_SECRET", "GoogleAds:ClientSecret", "Client secret OAuth do Google."),
        new(CategoriaConfiguracao.GoogleAds, "DeveloperToken", TipoConfiguracao.Segredo, true, null, "GOOGLE_ADS_DEVELOPER_TOKEN", "GoogleAds:DeveloperToken", "Developer token do Google Ads."),
        new(CategoriaConfiguracao.GoogleAds, "LoginCustomerId", TipoConfiguracao.Texto, false, "", "GOOGLE_ADS_LOGIN_CUSTOMER_ID", "GoogleAds:LoginCustomerId", "Customer ID gerente opcional."),
        new(CategoriaConfiguracao.GoogleAds, "RedirectUri", TipoConfiguracao.Url, false, "http://localhost:5173/configuracoes?googleAdsCallback=1", "GOOGLE_ADS_REDIRECT_URI", "GoogleAds:RedirectUri", "URL de callback OAuth."),
        new(CategoriaConfiguracao.GoogleAds, "AuthEndpoint", TipoConfiguracao.Url, false, "https://accounts.google.com/o/oauth2/v2/auth", "GOOGLE_ADS_AUTH_ENDPOINT", "GoogleAds:AuthEndpoint", "Endpoint de autorizacao OAuth."),
        new(CategoriaConfiguracao.GoogleAds, "TokenEndpoint", TipoConfiguracao.Url, false, "https://oauth2.googleapis.com/token", "GOOGLE_ADS_TOKEN_ENDPOINT", "GoogleAds:TokenEndpoint", "Endpoint de token OAuth."),
        new(CategoriaConfiguracao.GoogleAds, "UserInfoEndpoint", TipoConfiguracao.Url, false, "https://openidconnect.googleapis.com/v1/userinfo", "GOOGLE_ADS_USER_INFO_ENDPOINT", "GoogleAds:UserInfoEndpoint", "Endpoint de dados do usuario."),
        new(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", TipoConfiguracao.Url, false, "https://googleads.googleapis.com/v19", "GOOGLE_ADS_API_BASE_URL", "GoogleAds:ApiBaseUrl", "URL base da Google Ads API."),
        new(CategoriaConfiguracao.GoogleAds, "Scopes", TipoConfiguracao.Texto, false, "https://www.googleapis.com/auth/adwords openid email profile", "GOOGLE_ADS_SCOPES", "GoogleAds:Scopes", "Escopos OAuth solicitados."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultDailyBudget", TipoConfiguracao.Decimal, false, "10.00", "GOOGLE_ADS_DEFAULT_DAILY_BUDGET", "GoogleAds:DefaultDailyBudget", "Orcamento diario padrao."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultCountryCode", TipoConfiguracao.Texto, false, "BR", "GOOGLE_ADS_DEFAULT_COUNTRY_CODE", "GoogleAds:DefaultCountryCode", "Pais padrao."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultLanguageCode", TipoConfiguracao.Texto, false, "pt", "GOOGLE_ADS_DEFAULT_LANGUAGE_CODE", "GoogleAds:DefaultLanguageCode", "Idioma padrao."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultCurrencyCode", TipoConfiguracao.Texto, false, "BRL", "GOOGLE_ADS_DEFAULT_CURRENCY_CODE", "GoogleAds:DefaultCurrencyCode", "Moeda padrao."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultKeywordMatchType", TipoConfiguracao.Texto, false, "Phrase", "GOOGLE_ADS_DEFAULT_KEYWORD_MATCH_TYPE", "GoogleAds:DefaultKeywordMatchType", "Correspondencia padrao."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultCampaignStatus", TipoConfiguracao.Texto, false, "PAUSED", "GOOGLE_ADS_DEFAULT_CAMPAIGN_STATUS", "GoogleAds:DefaultCampaignStatus", "Status planejado."),
        new(CategoriaConfiguracao.GoogleAds, "EnableBroadMatch", TipoConfiguracao.Booleano, false, "false", "GOOGLE_ADS_ENABLE_BROAD_MATCH", "GoogleAds:EnableBroadMatch", "Permite broad match automatico."),
        new(CategoriaConfiguracao.GoogleAds, "DefaultCpcBid", TipoConfiguracao.Decimal, false, "", "GOOGLE_ADS_DEFAULT_CPC_BID", "GoogleAds:DefaultCpcBid", "CPC inicial opcional.")
    ];

    public static ConfigDefinition Get(CategoriaConfiguracao categoria, string key)
    {
        return Items.FirstOrDefault(x => x.Categoria == categoria && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Configuracao invalida.");
    }

    public static IReadOnlyList<ConfigDefinition> ByCategory(CategoriaConfiguracao categoria)
    {
        if (!Enum.IsDefined(categoria))
        {
            throw new ArgumentException("Categoria invalida.");
        }

        return Items.Where(x => x.Categoria == categoria).ToArray();
    }
}
