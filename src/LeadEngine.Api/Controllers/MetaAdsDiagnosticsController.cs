using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/meta-ads")]
public sealed class MetaAdsDiagnosticsController(
    IMetaAdsDiagnosticsService service,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("account")]
    public async Task<ActionResult<MetaAdAccountDto>> Account(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetAdAccountAsync(cancellationToken));
    }

    [HttpGet("campaigns")]
    public async Task<ActionResult<IReadOnlyList<MetaCampaignDto>>> Campaigns(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetCampaignsAsync(cancellationToken));
    }

    [HttpGet("adsets")]
    public async Task<ActionResult<IReadOnlyList<MetaAdSetDto>>> AdSets(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetAdSetsAsync(cancellationToken));
    }

    [HttpGet("ads")]
    public async Task<ActionResult<IReadOnlyList<MetaAdDto>>> Ads(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetAdsAsync(cancellationToken));
    }

    [HttpGet("creatives")]
    public async Task<ActionResult<IReadOnlyList<MetaCreativeDto>>> Creatives(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetCreativesAsync(cancellationToken));
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<CreateMetaCampaignResponse>> CreateCampaign(CreateMetaCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateCampaignAsync(request, cancellationToken));
    }

    [HttpDelete("campaigns/{campaignId}")]
    public async Task<ActionResult<DeleteMetaCampaignResponse>> DeleteCampaign(string campaignId, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.DeleteCampaignAsync(campaignId, cancellationToken));
    }

    [HttpPost("adsets")]
    public async Task<ActionResult<CreateMetaAdSetResponse>> CreateAdSet(CreateMetaAdSetRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateAdSetAsync(request, cancellationToken));
    }

    [HttpDelete("adsets/{adSetId}")]
    public async Task<ActionResult<DeleteMetaAdSetResponse>> DeleteAdSet(string adSetId, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.DeleteAdSetAsync(adSetId, cancellationToken));
    }

    [HttpPost("creatives")]
    public async Task<ActionResult<CreateMetaCreativeResponse>> CreateCreative(CreateMetaCreativeRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateCreativeAsync(request, cancellationToken));
    }

    [HttpDelete("creatives/{creativeId}")]
    public async Task<ActionResult<DeleteMetaCreativeResponse>> DeleteCreative(string creativeId, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.DeleteCreativeAsync(creativeId, cancellationToken));
    }

    [HttpPost("ads")]
    public async Task<ActionResult<CreateMetaAdResponse>> CreateAd(CreateMetaAdRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateAdAsync(request, cancellationToken));
    }

    [HttpDelete("ads/{adId}")]
    public async Task<ActionResult<DeleteMetaAdResponse>> DeleteAd(string adId, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.DeleteAdAsync(adId, cancellationToken));
    }

    private bool DiagnosticsEnabled()
    {
        return configuration.GetValue<bool>("MetaAds:DiagnosticsEnabled")
            || bool.TryParse(Environment.GetEnvironmentVariable("META_ADS_DIAGNOSTICS_ENABLED"), out var enabled) && enabled;
    }
}
