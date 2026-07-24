using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/publico/campanhas")]
public sealed class PublicoCampanhasController(ILeadService leadService) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<ActionResult<CampanhaPublicaResponse>> Obter(string slug, CancellationToken cancellationToken)
    {
        var campanha = await leadService.ObterCampanhaPublicaAsync(slug, cancellationToken);
        return campanha is null ? NotFound() : Ok(campanha);
    }

    [HttpPost("{slug}/leads")]
    [EnableRateLimiting("public-lead-capture")]
    public async Task<ActionResult<CapturarLeadPublicoResponse>> CapturarLead(string slug, CapturarLeadPublicoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await leadService.CapturarLeadPublicoAsync(slug, request, cancellationToken));
    }
}
