using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads/metricas")]
public sealed class GoogleAdsMetricasController(IGoogleAdsMetricsService service) : ControllerBase
{
    [HttpPost("publicacoes/{publicacaoId:guid}/sincronizar")]
    public async Task<ActionResult<GoogleAdsSincronizacaoResponse>> SincronizarPublicacao(Guid publicacaoId, GoogleAdsPeriodoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.SincronizarPublicacaoAsync(publicacaoId, request, cancellationToken));
    }

    [HttpPost("sincronizar")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsSincronizacaoResponse>>> SincronizarTodas(GoogleAdsPeriodoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.SincronizarTodasAsync(request, cancellationToken));
    }

    [HttpGet("publicacoes/{publicacaoId:guid}")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsMetricaDiariaResponse>>> PorPublicacao(Guid publicacaoId, [FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, CancellationToken cancellationToken)
    {
        return Ok(await service.ListarPorPublicacaoAsync(publicacaoId, dataInicial, dataFinal, cancellationToken));
    }

    [HttpGet("campanhas/{campanhaId:guid}")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDashboardCampanhaResponse>>> PorCampanha(Guid campanhaId, [FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, CancellationToken cancellationToken)
    {
        return Ok(await service.RankingAsync(dataInicial, dataFinal, campanhaId, null, cancellationToken));
    }

    [HttpGet("resumo")]
    public async Task<ActionResult<GoogleAdsDashboardResumoResponse>> Resumo([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
    {
        return Ok(await service.ResumoAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));
    }

    [HttpGet("evolucao")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsEvolucaoResponse>>> Evolucao([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
    {
        return Ok(await service.EvolucaoAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));
    }

    [HttpGet("ranking")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDashboardCampanhaResponse>>> Ranking([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
    {
        return Ok(await service.RankingAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));
    }
}
