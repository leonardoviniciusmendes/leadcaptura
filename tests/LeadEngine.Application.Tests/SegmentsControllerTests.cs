using LeadEngine.Api.Controllers;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Application.Tests;

public sealed class SegmentsControllerTests
{
    [Fact]
    public async Task GetSegments_RetornaSomenteAtivos()
    {
        var controller = new SegmentsController(new Repo([
            Segment("oficina", true),
            Segment("inativo", false)
        ]));

        var result = await controller.List(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<SegmentResponse>>(ok.Value);
        Assert.Single(items);
        Assert.Equal("oficina", items[0].Slug);
    }

    [Fact]
    public async Task GetSegment_SegmentoInativoNaoRetorna()
    {
        var controller = new SegmentsController(new Repo([Segment("inativo", false)]));

        var result = await controller.GetBySlug("inativo", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetSegments_DerivaLegacyBriefingDaConfiguracao()
    {
        var controller = new SegmentsController(new Repo([
            Segment("planos-saude", true, """{"legacyCompatibility":true,"defaultGoal":"lead_generation_whatsapp"}""")
        ]));

        var result = await controller.List(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var item = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<SegmentResponse>>(ok.Value));
        Assert.True(item.UsesLegacyBriefing);
        Assert.Equal("lead_generation_whatsapp", item.DefaultCampaignGoal);
    }

    private static Segment Segment(string slug, bool active, string? config = null)
    {
        return new Segment
        {
            Id = Guid.NewGuid(),
            Name = slug,
            Slug = slug,
            TemplateKey = "local_service_lead_generation",
            IsActive = active,
            DefaultConfigJson = config
        };
    }

    private sealed class Repo(IReadOnlyList<Segment> segments) : ISegmentRepository
    {
        public Task<Segment?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(segments.FirstOrDefault(x => x.Slug == slug));
        }

        public Task<Segment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(segments.FirstOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<Segment>> ListActiveAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Segment>>(segments.Where(x => x.IsActive).ToArray());
        }

        public Task<IReadOnlyList<Segment>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(segments);
        }

        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(segments.Any(x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value)));
        }

        public Task<int> CountCampaignsAsync(Guid segmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task AddAsync(Segment segment, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
