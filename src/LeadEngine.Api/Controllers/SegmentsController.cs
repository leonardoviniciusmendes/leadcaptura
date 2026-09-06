using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/segments")]
public sealed class SegmentsController(ISegmentRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SegmentResponse>>> List(CancellationToken cancellationToken)
    {
        var segments = await repository.ListActiveAsync(cancellationToken);
        return Ok(segments.Select(SegmentMapping.ToResponse).ToArray());
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<SegmentResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var segment = await repository.GetBySlugAsync(slug, cancellationToken);
        return segment is null || !segment.IsActive
            ? NotFound()
            : Ok(SegmentMapping.ToResponse(segment));
    }
}
