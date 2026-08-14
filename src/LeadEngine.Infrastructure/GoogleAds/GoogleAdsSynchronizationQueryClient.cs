using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsSynchronizationQueryClient(
    GoogleAdsGaqlClient gaqlClient,
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver,
    GoogleAdsExceptionFormatter exceptionFormatter) : IGoogleAdsSynchronizationQueryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsRemoteStatusSnapshot> GetRemoteStatusAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
    {
        var campaign = resources.FirstOrDefault(x => x.TipoRecurso == "Campaign")?.ResourceName;
        if (string.IsNullOrWhiteSpace(campaign))
        {
            return new GoogleAdsRemoteStatusSnapshot("", null, null, null, null, null, null, [], [], null, ["Campaign resource name ausente."], [], null);
        }
        var (requestId, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, GoogleAdsGaqlQueries.CampaignStatus(campaign), cancellationToken);
        using (body)
        {
            var found = body.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0;
            return found
                ? new GoogleAdsRemoteStatusSnapshot(campaign, null, "PAUSED", null, null, null, null, [], [], null, [], [], requestId)
                : new GoogleAdsRemoteStatusSnapshot(campaign, null, null, null, null, null, null, [], [], null, [campaign], [], requestId);
        }
    }

    public async Task SetCampaignStatusAsync(string customerId, string accessToken, string developerToken, string campaignResourceName, string status, CancellationToken cancellationToken)
    {
        var normalizedStatus = status.Equals("ENABLED", StringComparison.OrdinalIgnoreCase) ? "ENABLED" : "PAUSED";
        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var body = new
        {
            mutateOperations = new object[]
            {
                new
                {
                    campaignOperation = new
                    {
                        update = new
                        {
                            resourceName = campaignResourceName,
                            status = normalizedStatus
                        },
                        updateMask = "status"
                    }
                }
            },
            partialFailure = false,
            validateOnly = false
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:mutate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GoogleAdsDiagnosticException(exceptionFormatter.FromRestError(text, requestId, ((int)response.StatusCode).ToString(), "Nao foi possivel alterar status remoto da campanha Google Ads."));
        }
    }
}
