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

    private bool DiagnosticsEnabled()
    {
        return configuration.GetValue<bool>("MetaAds:DiagnosticsEnabled")
            || bool.TryParse(Environment.GetEnvironmentVariable("META_ADS_DIAGNOSTICS_ENABLED"), out var enabled) && enabled;
    }
}
