using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/admin/segments")]
public sealed class AdminSegmentsController(ISegmentRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminSegmentResponse>>> List(CancellationToken cancellationToken)
    {
        var segments = await repository.ListAsync(cancellationToken);
        var response = new List<AdminSegmentResponse>(segments.Count);
        foreach (var segment in segments)
        {
            response.Add(SegmentMapping.ToAdminResponse(segment, await repository.CountCampaignsAsync(segment.Id, cancellationToken)));
        }

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminSegmentResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var segment = await repository.GetByIdAsync(id, cancellationToken);
        if (segment is null)
        {
            return NotFound();
        }

        var campaignsCount = await repository.CountCampaignsAsync(segment.Id, cancellationToken);
        return Ok(SegmentMapping.ToAdminResponse(segment, campaignsCount));
    }

    [HttpPost]
    public async Task<ActionResult<AdminSegmentResponse>> Create(UpsertSegmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var now = DateTime.UtcNow;
        var segment = new Segment
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            Slug = NormalizeSlug(request.Slug),
            Description = NormalizeOptional(request.Description),
            TemplateKey = request.TemplateKey!.Trim(),
            DefaultConfigJson = NormalizeOptional(request.DefaultConfigJson),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(segment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var response = SegmentMapping.ToAdminResponse(segment, 0);
        return CreatedAtAction(nameof(Get), new { id = segment.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminSegmentResponse>> Update(Guid id, UpsertSegmentRequest request, CancellationToken cancellationToken)
    {
        var segment = await repository.GetByIdAsync(id, cancellationToken);
        if (segment is null)
        {
            return NotFound();
        }

        var validation = await ValidateAsync(request, id, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        segment.Name = request.Name!.Trim();
        segment.Slug = NormalizeSlug(request.Slug);
        segment.Description = NormalizeOptional(request.Description);
        segment.TemplateKey = request.TemplateKey!.Trim();
        segment.DefaultConfigJson = NormalizeOptional(request.DefaultConfigJson);
        segment.IsActive = request.IsActive;
        segment.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        var campaignsCount = await repository.CountCampaignsAsync(segment.Id, cancellationToken);
        return Ok(SegmentMapping.ToAdminResponse(segment, campaignsCount));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<AdminSegmentResponse>> UpdateStatus(Guid id, UpdateSegmentStatusRequest request, CancellationToken cancellationToken)
    {
        var segment = await repository.GetByIdAsync(id, cancellationToken);
        if (segment is null)
        {
            return NotFound();
        }

        segment.IsActive = request.IsActive;
        segment.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        var campaignsCount = await repository.CountCampaignsAsync(segment.Id, cancellationToken);
        return Ok(SegmentMapping.ToAdminResponse(segment, campaignsCount));
    }

    private async Task<ActionResult?> ValidateAsync(UpsertSegmentRequest request, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem("Nome do segmento e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return ValidationProblem("Slug do segmento e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.TemplateKey))
        {
            return ValidationProblem("TemplateKey do segmento e obrigatorio.");
        }

        if (!IsValidJson(request.DefaultConfigJson))
        {
            return ValidationProblem("DefaultConfigJson deve ser um JSON valido.");
        }

        var slug = NormalizeSlug(request.Slug);
        if (await repository.SlugExistsAsync(slug, excludingId, cancellationToken))
        {
            return Conflict(new { mensagem = "Ja existe um segmento com este slug." });
        }

        return null;
    }

    private static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizeSlug(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
