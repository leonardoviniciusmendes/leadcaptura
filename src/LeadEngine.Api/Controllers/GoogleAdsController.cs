using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads")]
public sealed class GoogleAdsController(IGoogleAdsConnectionService service) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<GoogleAdsStatusResponse>> Status(CancellationToken cancellationToken)
    {
        return Ok(await service.ObterStatusAsync(cancellationToken));
    }

    [HttpGet("auth-url")]
    public async Task<ActionResult<GoogleAdsAuthUrlResponse>> AuthUrl(CancellationToken cancellationToken)
    {
        return Ok(await service.GerarAuthUrlAsync(cancellationToken));
    }

    [HttpPost("oauth/callback")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsContaResponse>>> OAuthCallback(GoogleAdsOAuthCallbackRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.ConcluirOAuthAsync(request, cancellationToken));
    }

    [HttpGet("contas")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsContaResponse>>> Contas(CancellationToken cancellationToken)
    {
        return Ok(await service.ListarContasAsync(cancellationToken));
    }

    [HttpPost("contas/{id:guid}/selecionar")]
    public async Task<ActionResult<GoogleAdsContaResponse>> SelecionarConta(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.SelecionarContaPadraoAsync(id, cancellationToken));
    }

    [HttpPost("testar")]
    public async Task<ActionResult<GoogleAdsTestarResponse>> Testar(GoogleAdsTestarRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.TestarAsync(request, cancellationToken));
    }
}
