using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsRemoteValidationService(
    IGoogleAdsOperationBuilder operationBuilder,
    IGoogleAdsMutationClient mutationClient,
    IGoogleAdsTokenService tokenService,
    IConfigurationResolver resolver) : IGoogleAdsRemoteValidationService
{
    public async Task<GoogleAdsMutationResult> ValidarAsync(GoogleAdsPlanoPublicacao preview, GoogleAdsConta conta, CancellationToken cancellationToken)
    {
        var token = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        var developerToken = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "DeveloperToken", cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(developerToken))
        {
            return new GoogleAdsMutationResult(false, null, [], [new GoogleAdsPublicationErrorDto("developer_token_missing", "DeveloperToken nao configurado.", null, null, "DeveloperToken", null, null, false, "Configure GoogleAds.DeveloperToken.")], false);
        }
        var plan = await operationBuilder.BuildAsync(preview, conta.CustomerId, cancellationToken);
        return await mutationClient.MutateAsync(conta.CustomerId, token, developerToken, plan, validateOnly: true, cancellationToken);
    }
}
