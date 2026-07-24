using LeadEngine.Application.DTOs;
using LeadEngine.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/campanhas")]
public sealed class CampanhasController(CampanhaService campanhaService) : ControllerBase
{
    [HttpPost("gerar")]
    public async Task<ActionResult<CampanhaResponse>> Gerar(GerarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var campanha = await campanhaService.GerarCampanhaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = campanha.Id }, campanha);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CampanhaResponse>>> Listar(CancellationToken cancellationToken)
    {
        return Ok(await campanhaService.ListarCampanhasAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampanhaResponse>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await campanhaService.ObterCampanhaPorIdAsync(id, cancellationToken);
        return campanha is null ? NotFound() : Ok(campanha);
    }

    [HttpPut("{id:guid}/revisao")]
    public async Task<ActionResult<CampanhaResponse>> Revisar(Guid id, RevisarCampanhaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await campanhaService.RevisarCampanhaAsync(id, request, cancellationToken));
    }
}
