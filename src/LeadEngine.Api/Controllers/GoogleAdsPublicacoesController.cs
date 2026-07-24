using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads/publicacoes")]
public sealed class GoogleAdsPublicacoesController(IGoogleAdsPublishingService service, IGoogleAdsSynchronizationService synchronizationService) : ControllerBase
{
    [HttpPost("preview/{previewId:guid}/validar-remotamente")]
    public async Task<ActionResult<GoogleAdsRemoteValidationResponse>> ValidarRemotamente(Guid previewId, CancellationToken cancellationToken)
    {
        return Ok(await service.ValidarRemotamenteAsync(previewId, cancellationToken));
    }

    [HttpPost("preview/{previewId:guid}/dry-run")]
    public async Task<ActionResult<GoogleAdsDryRunResponse>> DryRun(Guid previewId, CancellationToken cancellationToken)
    {
        return Ok(await service.DryRunAsync(previewId, cancellationToken));
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { sucesso = false, code = "google_ads_publication_disabled", mensagem = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reconciliar")]
    public async Task<ActionResult<GoogleAdsReconciliationResponse>> Reconciliar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ReconciliarAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/sincronizar")]
    public async Task<ActionResult<GoogleAdsSincronizacaoResponse>> Sincronizar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await synchronizationService.SincronizarPublicacaoAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/status-remoto")]
    public async Task<ActionResult<GoogleAdsStatusRemotoResponse>> StatusRemoto(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await synchronizationService.ObterStatusRemotoAsync(id, cancellationToken));
    }

    [HttpPost("sincronizar")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsSincronizacaoResponse>>> SincronizarTodas(CancellationToken cancellationToken)
    {
        return Ok(await synchronizationService.SincronizarTodasAsync(cancellationToken));
    }

    [HttpPost("{id:guid}/pausar")]
    public async Task<ActionResult<GoogleAdsPublicationResponse>> Pausar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await synchronizationService.PausarAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/ativar")]
    public async Task<ActionResult<GoogleAdsPublicationResponse>> Ativar(Guid id, GoogleAdsStatusActionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await synchronizationService.AtivarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/atualizar")]
    public ActionResult Atualizar(Guid id, GoogleAdsAtualizacaoRequest request)
    {
        return BadRequest(new { sucesso = false, mensagem = "Atualizacao remota suportara inicialmente orcamento e RSA apos validacao dedicada. Nesta versao, gere novo preview e execute dry run/validate_only." });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoogleAdsPublicationResponse>> Obter(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/historico")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsPublicationHistoryResponse>>> Historico(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.HistoricoAsync(id, cancellationToken));
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
