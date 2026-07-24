using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads/publicacoes")]
public sealed class GoogleAdsPublicacoesController(IGoogleAdsPublishingService service) : ControllerBase
{
    [HttpPost("preview/{previewId:guid}/validar-remotamente")]
    public async Task<ActionResult<GoogleAdsRemoteValidationResponse>> ValidarRemotamente(Guid previewId, CancellationToken cancellationToken)
    {
        return Ok(await service.ValidarRemotamenteAsync(previewId, cancellationToken));
    }

    [HttpPost("preview/{previewId:guid}/preparar")]
    public async Task<ActionResult<GoogleAdsPreparePublicationResponse>> Preparar(Guid previewId, CancellationToken cancellationToken)
    {
        return Ok(await service.PrepararAsync(previewId, cancellationToken));
    }

    [HttpPost("preview/{previewId:guid}/publicar")]
    public async Task<ActionResult<GoogleAdsPublicationResponse>> Publicar(Guid previewId, GoogleAdsPublishRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.PublicarAsync(previewId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { sucesso = false, code = "google_ads_publication_conflict", mensagem = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reconciliar")]
    public async Task<ActionResult<GoogleAdsReconciliationResponse>> Reconciliar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ReconciliarAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoogleAdsPublicationResponse>> Obter(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterAsync(id, cancellationToken));
    }

    [HttpGet("campanha/{campanhaId:guid}")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsPublicationResponse>>> PorCampanha(Guid campanhaId, CancellationToken cancellationToken)
    {
        return Ok(await service.ListarPorCampanhaAsync(campanhaId, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsPublicationResponse>>> Listar(
        [FromQuery] StatusPublicacaoGoogleAds? status,
        [FromQuery] Guid? campanhaId,
        [FromQuery] Guid? contaId,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarAsync(new GoogleAdsPublicationQuery(status, campanhaId, contaId, dataInicial, dataFinal), cancellationToken));
    }
}
