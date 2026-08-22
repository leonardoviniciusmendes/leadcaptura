using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/metaads")]
public sealed class MetaAdsController(
    IMetaAdsConnectionService service,
    IMetaAdsAssetService assetService,
    IMetaAdsPreviewService previewService,
    IMetaAdsPublicationPreparationService preparationService,
    IMetaAdsPublishingService publishingService) : ControllerBase
{
    private const long MaxImageUploadBytes = 10 * 1024 * 1024;

    [HttpGet("status")]
    public async Task<ActionResult<MetaAdsStatusResponse>> Status(CancellationToken cancellationToken)
    {
        return Ok(await service.ObterStatusAsync(cancellationToken));
    }

    [HttpGet("auth-url")]
    public async Task<ActionResult<MetaAdsAuthUrlResponse>> AuthUrl([FromQuery] bool publicacao, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GerarAuthUrlAsync(publicacao, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPost("oauth/callback")]
    public async Task<ActionResult<MetaAdsOAuthCallbackResponse>> OAuthCallback(MetaAdsOAuthCallbackRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.ConcluirOAuthAsync(request, cancellationToken));
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

    [HttpPost("disconnect")]
    public async Task<ActionResult<MetaAdsStatusResponse>> Disconnect(CancellationToken cancellationToken)
    {
        return Ok(await service.DesconectarAsync(cancellationToken));
    }

    [HttpDelete("disconnect")]
    public async Task<ActionResult<MetaAdsStatusResponse>> DisconnectDelete(CancellationToken cancellationToken)
    {
        return Ok(await service.DesconectarAsync(cancellationToken));
    }

    [HttpGet("businesses")]
    public async Task<ActionResult<MetaAdsAssetListResponse<MetaAdsBusinessResponse>>> Businesses(CancellationToken cancellationToken)
    {
        try { return Ok(await assetService.ListarBusinessesAsync(cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
    }

    [HttpGet("businesses/{businessId}/ad-accounts")]
    public async Task<ActionResult<MetaAdsAssetListResponse<MetaAdsAdAccountResponse>>> AdAccounts(string businessId, CancellationToken cancellationToken)
    {
        try { return Ok(await assetService.ListarAdAccountsAsync(businessId, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
    }

    [HttpGet("pages")]
    public async Task<ActionResult<MetaAdsAssetListResponse<MetaAdsPageResponse>>> Pages(CancellationToken cancellationToken)
    {
        try { return Ok(await assetService.ListarPagesAsync(cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
    }

    [HttpGet("pages/{pageId}/instagram")]
    public async Task<ActionResult<MetaAdsAssetListResponse<MetaAdsInstagramAccountResponse>>> Instagram(string pageId, CancellationToken cancellationToken)
    {
        try { return Ok(await assetService.ObterInstagramAsync(pageId, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
    }

    [HttpGet("ad-accounts/{adAccountId}/pixels")]
    public async Task<ActionResult<MetaAdsAssetListResponse<MetaAdsPixelResponse>>> Pixels(string adAccountId, CancellationToken cancellationToken)
    {
        try { return Ok(await assetService.ListarPixelsAsync(adAccountId, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
    }

    [HttpGet("assets-selection")]
    public async Task<ActionResult<MetaAdsAssetSelectionResponse>> AssetsSelection(CancellationToken cancellationToken)
    {
        try { return Ok(await assetService.ObterSelecaoAsync(cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
    }

    [HttpPut("assets-selection")]
    public async Task<ActionResult<MetaAdsAssetSelectionResponse>> AssetsSelection(MetaAdsAssetSelectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await assetService.SalvarSelecaoAsync(request, cancellationToken));
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

    [HttpPost("preview")]
    public async Task<ActionResult<MetaAdsPreviewResponse>> Preview(MetaAdsPreviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await previewService.GerarAsync(request, cancellationToken));
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

    [HttpGet("targeting/locations")]
    public async Task<ActionResult<MetaAdsLocationSearchResponse>> TargetingLocations([FromQuery] string query, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await preparationService.BuscarLocalizacoesAsync(query ?? string.Empty, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPut("publication-targeting")]
    public async Task<ActionResult<MetaAdsLocationResponse>> PublicationTargeting(MetaAdsTargetingSelectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await preparationService.SalvarTargetingAsync(request, cancellationToken));
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

    [HttpPost("campaigns/{campanhaId:guid}/image")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageUploadBytes)]
    public async Task<ActionResult<MetaAdsUploadImageResponse>> UploadImage(Guid campanhaId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { sucesso = false, mensagem = "Imagem obrigatoria." });
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        try
        {
            return Ok(await preparationService.EnviarImagemAsync(campanhaId, file.FileName, file.ContentType, memory.ToArray(), cancellationToken));
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

    [HttpGet("campaigns/{campanhaId:guid}/publication")]
    public async Task<ActionResult<MetaAdsPublicationStatusResponse>> Publication(Guid campanhaId, CancellationToken cancellationToken)
    {
        return Ok(await publishingService.ObterPorCampanhaAsync(campanhaId, cancellationToken));
    }

    [HttpPost("campaigns/{campanhaId:guid}/publish")]
    public async Task<ActionResult<MetaAdsPublicacaoResponse>> Publish(Guid campanhaId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await publishingService.PublicarAsync(campanhaId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPost("publicacoes/{id:guid}/retry")]
    public async Task<ActionResult<MetaAdsPublicacaoResponse>> Retry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await publishingService.RetentarAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { sucesso = false, mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
    }
}
