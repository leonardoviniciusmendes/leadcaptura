using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads/dashboard")]
public sealed class GoogleAdsDashboardController(IGoogleAdsMetricsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GoogleAdsDashboardResumoResponse>> Resumo([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
        => Ok(await service.ResumoAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));

    [HttpGet("evolucao")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsEvolucaoResponse>>> Evolucao([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
        => Ok(await service.EvolucaoAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));

    [HttpGet("campanhas")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDashboardCampanhaResponse>>> Campanhas([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
        => Ok(await service.RankingAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));

    [HttpGet("atribuicao")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsAtribuicaoResponse>>> Atribuicao([FromQuery] DateOnly? dataInicial, [FromQuery] DateOnly? dataFinal, [FromQuery] Guid? campanhaId, [FromQuery] Guid? contaId, CancellationToken cancellationToken)
        => Ok(await service.AtribuicaoAsync(dataInicial, dataFinal, campanhaId, contaId, cancellationToken));
}
