using LeadEngine.Application.Common;
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
    LeadConsultaService leadConsultaService,
    CreativeAssetService creativeAssetService) : ControllerBase
{
    private const long MaxCreativeAssetsUploadBytes = 128 * 1024 * 1024;

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
    public async Task<ActionResult<AprovarCampanhaResponse>> Aprovar(Guid id, AprovarCampanhaRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await reviewService.AprovarCampanhaAsync(id, request ?? new AprovarCampanhaRequest(), cancellationToken));
        }
        catch (CreativeQualityGateException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = "A imagem principal nao passou no quality gate criativo.", creativeQualityGate = ex.Gate });
        }
    }

    [HttpGet("{id:guid}/creative-quality-gate")]
    public async Task<ActionResult<CreativeQualityGateResponse>> CreativeQualityGate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await reviewService.ObterCreativeQualityGateAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
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

    [HttpGet("{id:guid}/creative-assets")]
    public async Task<ActionResult<IReadOnlyList<CreativeAssetResponse>>> CreativeAssets(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await creativeAssetService.ListAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPost("{id:guid}/creative-assets")]
    [RequestSizeLimit(MaxCreativeAssetsUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxCreativeAssetsUploadBytes)]
    public async Task<ActionResult<CreativeAssetUploadResponse>> UploadCreativeAssets(Guid id, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest(new { sucesso = false, mensagem = "Envie pelo menos uma imagem." });
        }

        try
        {
            var items = new List<CreativeAssetUploadItem>();
            foreach (var file in files)
            {
                items.Add(new CreativeAssetUploadItem(file.FileName, file.ContentType, file.OpenReadStream(), file.Length));
            }

            return Ok(await creativeAssetService.UploadAsync(id, items, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpGet("{id:guid}/creative-assets/{assetId:guid}/content")]
    public async Task<IActionResult> CreativeAssetContent(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        try
        {
            var content = await creativeAssetService.GetContentAsync(id, assetId, cancellationToken);
            return File(content.Content, content.MimeType);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpGet("{id:guid}/creative-assets/{assetId:guid}/thumbnail")]
    public async Task<IActionResult> CreativeAssetThumbnail(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        try
        {
            var content = await creativeAssetService.GetThumbnailAsync(id, assetId, cancellationToken);
            return File(content.Content, content.MimeType);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPost("{id:guid}/creative-assets/{assetId:guid}/analyze")]
    public async Task<ActionResult<CreativeAssetAnalysisResponse>> AnalyzeCreativeAsset(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await creativeAssetService.AnalyzeAsync(id, assetId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPut("{id:guid}/creative-assets/{assetId:guid}/select")]
    public async Task<ActionResult<CreativeAssetResponse>> SelectCreativeAsset(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await creativeAssetService.SelectAsync(id, assetId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/creative-assets/{assetId:guid}")]
    public async Task<IActionResult> DeleteCreativeAsset(Guid id, Guid assetId, CancellationToken cancellationToken)
    {
        try
        {
            await creativeAssetService.RemoverAsync(id, assetId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
        catch (IOException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = $"Nao foi possivel remover o arquivo da imagem: {ex.Message}" });
        }
    }
}
