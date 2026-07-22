using LeadEngine.Application.DTOs;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LeadsController(
    LeadCaptureService captureService,
    LeadConsultaService consultaService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("lead-capture")]
    public async Task<ActionResult<CapturaLeadResponse>> Criar(CriarLeadRequest request, CancellationToken cancellationToken)
    {
        var response = await captureService.CapturarAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadResponse>>> Listar(
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        [FromQuery] TipoLead? tipo,
        [FromQuery] StatusLead? status,
        [FromQuery] string? campanha,
        [FromQuery] string? landingPage,
        [FromQuery] string? whatsApp,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new LeadQuery(dataInicial, dataFinal, tipo, status, campanha, landingPage, whatsApp, pagina, tamanhoPagina);
        return Ok(await consultaService.ListarAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDetalheResponse>> Obter(Guid id, CancellationToken cancellationToken)
    {
        var lead = await consultaService.ObterAsync(id, cancellationToken);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPost("{id:guid}/reenviar")]
    public async Task<ActionResult<CapturaLeadResponse>> Reenviar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await captureService.ReenviarAsync(id, cancellationToken));
    }
}
