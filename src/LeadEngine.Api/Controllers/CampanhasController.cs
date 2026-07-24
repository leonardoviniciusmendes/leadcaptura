using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/campanhas")]
public sealed class CampanhasController(
    CampanhaService campanhaService,
    ICampaignReviewService reviewService,
    ICampaignPublicationService publicationService,
    LeadConsultaService leadConsultaService) : ControllerBase
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
        return Ok(await reviewService.RevisarCampanhaAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/revisao")]
    public async Task<ActionResult<CampanhaResponse>> ObterRevisao(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await reviewService.ObterRevisaoAsync(id, cancellationToken);
        return campanha is null ? NotFound() : Ok(campanha);
    }

    [HttpPost("{id:guid}/regenerar")]
    public async Task<ActionResult<CampanhaResponse>> Regenerar(Guid id, RegenerarCampanhaSecaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await reviewService.RegenerarSecaoAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<ActionResult<CampanhaResponse>> Aprovar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await reviewService.AprovarCampanhaAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/historico-revisoes")]
    public async Task<ActionResult<IReadOnlyList<CampanhaRevisaoHistoricoResponse>>> HistoricoRevisoes(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await reviewService.ListarHistoricoAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/publicar")]
    public async Task<ActionResult<CampanhaPublicacaoResponse>> Publicar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await publicationService.PublicarAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/despublicar")]
    public async Task<ActionResult<CampanhaPublicacaoResponse>> Despublicar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await publicationService.DespublicarAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/publicacao")]
    public async Task<ActionResult<CampanhaPublicacaoResponse>> Publicacao(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await publicationService.ObterPublicacaoAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/leads")]
    public async Task<ActionResult<PagedResult<LeadResponse>>> Leads(
        Guid id,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        [FromQuery] string? telefone,
        [FromQuery] LeadEngine.Domain.Enums.TipoContratacaoLead? tipoContratacao,
        [FromQuery] string? origem,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await leadConsultaService.ListarAsync(new LeadQuery(id, dataInicial, dataFinal, null, null, null, null, telefone, tipoContratacao, origem, pagina, tamanhoPagina), cancellationToken));
    }
}
