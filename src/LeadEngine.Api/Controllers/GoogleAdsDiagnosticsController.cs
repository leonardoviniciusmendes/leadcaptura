using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/googleads/diagnostics")]
public sealed class GoogleAdsDiagnosticsController(
    IGoogleAdsDiagnosticsService service,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("account")]
    public async Task<ActionResult<GoogleAdsDiagnosticAccountResponse>> Account(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetAccountAsync(cancellationToken));
    }

    [HttpGet("campaigns")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>>> Campaigns(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetCampaignsAsync(cancellationToken));
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<CreateGoogleAdsDiagnosticCampaignResponse>> CreateCampaign(CreateGoogleAdsDiagnosticCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateCampaignAsync(request, cancellationToken));
    }

    [HttpGet("adgroups")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>>> AdGroups(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetAdGroupsAsync(cancellationToken));
    }

    [HttpPost("adgroups")]
    public async Task<ActionResult<CreateGoogleAdsDiagnosticAdGroupResponse>> CreateAdGroup(CreateGoogleAdsDiagnosticAdGroupRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateAdGroupAsync(request, cancellationToken));
    }

    [HttpGet("keywords")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>>> Keywords(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetKeywordsAsync(cancellationToken));
    }

    [HttpPost("keywords")]
    public async Task<ActionResult<CreateGoogleAdsDiagnosticKeywordsResponse>> CreateKeywords(CreateGoogleAdsDiagnosticKeywordsRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateKeywordsAsync(request, cancellationToken));
    }

    [HttpGet("responsive-search-ads")]
    public async Task<ActionResult<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>>> ResponsiveSearchAds(CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.GetResponsiveSearchAdsAsync(cancellationToken));
    }

    [HttpPost("responsive-search-ads")]
    public async Task<ActionResult<CreateGoogleAdsDiagnosticResponsiveSearchAdResponse>> CreateResponsiveSearchAd(CreateGoogleAdsDiagnosticResponsiveSearchAdRequest request, CancellationToken cancellationToken)
    {
        if (!DiagnosticsEnabled()) return NotFound();
        return Ok(await service.CreateResponsiveSearchAdAsync(request, cancellationToken));
    }

    private bool DiagnosticsEnabled()
    {
        return configuration.GetValue<bool>("GoogleAds:DiagnosticsEnabled")
            || bool.TryParse(Environment.GetEnvironmentVariable("GOOGLE_ADS_DIAGNOSTICS_ENABLED"), out var enabled) && enabled;
    }
}
