using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads/preview")]
public sealed class GoogleAdsPreviewController(IGoogleAdsPreviewService service) : ControllerBase
{
    [HttpPost("campanhas/{campanhaId:guid}")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> Gerar(Guid campanhaId, CancellationToken cancellationToken)
    {
        return Ok(await service.GerarOuAtualizarAsync(campanhaId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> Obter(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterAsync(id, cancellationToken));
    }

    [HttpGet("campanhas/{campanhaId:guid}")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> ObterPorCampanha(Guid campanhaId, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterPorCampanhaAsync(campanhaId, cancellationToken));
    }

    [HttpPost("{id:guid}/validar")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> Validar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ValidarAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> Atualizar(Guid id, AtualizarGoogleAdsPreviewRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/sugerir-ajustes")]
    public async Task<ActionResult<GoogleAdsCopySuggestionResponse>> SugerirAjustes(Guid id, GoogleAdsSugerirAjustesRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.SugerirAjustesAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/aplicar-sugestao")]
    public async Task<ActionResult<GoogleAdsPreviewResponse>> AplicarSugestao(Guid id, AplicarGoogleAdsSugestaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AplicarSugestaoAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/payload")]
    public async Task<ActionResult<GoogleAdsPreviewPayload>> Payload(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterPayloadAsync(id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(id, cancellationToken);
        return NoContent();
    }
}
