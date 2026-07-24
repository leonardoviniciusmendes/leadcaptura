using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads")]
public sealed class GoogleAdsAnalisesController(IGoogleAdsOptimizationService service) : ControllerBase
{
    [HttpPost("publicacoes/{id:guid}/analisar")]
    public async Task<ActionResult<GoogleAdsAnaliseResponse>> Analisar(Guid id, GoogleAdsPeriodoRequest request, CancellationToken cancellationToken)
        => Ok(await service.AnalisarAsync(id, request, cancellationToken));

    [HttpGet("publicacoes/{id:guid}/analises")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsAnaliseResponse>>> Listar(Guid id, CancellationToken cancellationToken)
        => Ok(await service.ListarAsync(id, cancellationToken));

    [HttpGet("analises/{id:guid}")]
    public async Task<ActionResult<GoogleAdsAnaliseResponse>> Obter(Guid id, CancellationToken cancellationToken)
        => Ok(await service.ObterAsync(id, cancellationToken));

    [HttpPost("analises/{id:guid}/criar-preview")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> CriarPreview(Guid id, GoogleAdsCriarPreviewPorAnaliseRequest request, CancellationToken cancellationToken)
        => Ok(await service.CriarPreviewAsync(id, request, cancellationToken));
}
