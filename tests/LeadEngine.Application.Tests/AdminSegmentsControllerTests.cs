using LeadEngine.Api.Controllers;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Application.Tests;

public sealed class AdminSegmentsControllerTests
{
    [Fact]
    public async Task Create_CriaSegmento()
    {
        var repo = new Repo();
        var controller = new AdminSegmentsController(repo);

        var result = await controller.Create(new UpsertSegmentRequest(
            "Estetica",
            "estetica",
            "Clinicas de estetica",
            "local_service_lead_generation",
            """{"ui":{"legacyBriefing":false}}""",
            true), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AdminSegmentResponse>(created.Value);
        Assert.Equal("estetica", response.Slug);
        Assert.True(response.IsActive);
        Assert.Single(repo.Segments);
        Assert.Equal(1, repo.SaveCalls);
    }

    [Fact]
    public async Task Create_SlugDuplicadoRetornaConflict()
    {
        var repo = new Repo([Segment("estetica")]);
        var controller = new AdminSegmentsController(repo);

        var result = await controller.Create(new UpsertSegmentRequest(
            "Estetica 2",
            "estetica",
            null,
            "local_service_lead_generation",
            null,
            true), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_JsonInvalidoRetornaErroValidacao()
    {
        var controller = new AdminSegmentsController(new Repo());

        var result = await controller.Create(new UpsertSegmentRequest(
            "Estetica",
            "estetica",
            null,
            "local_service_lead_generation",
            "{invalido",
            true), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_EditaSegmento()
    {
        var segment = Segment("estetica");
        var repo = new Repo([segment]);
        var controller = new AdminSegmentsController(repo);

        var result = await controller.Update(segment.Id, new UpsertSegmentRequest(
            "Estetica premium",
            "estetica-premium",
            "Atualizado",
            "appointment_booking",
            """{"defaultGoal":"agendar"}""",
            false), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AdminSegmentResponse>(ok.Value);
        Assert.Equal("Estetica premium", response.Name);
        Assert.Equal("estetica-premium", response.Slug);
        Assert.Equal("appointment_booking", response.TemplateKey);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task UpdateStatus_DesativaSegmento()
    {
        var segment = Segment("estetica");
        var repo = new Repo([segment]);
        var controller = new AdminSegmentsController(repo);

        var result = await controller.UpdateStatus(segment.Id, new UpdateSegmentStatusRequest(false), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AdminSegmentResponse>(ok.Value);
        Assert.False(response.IsActive);
        Assert.False(segment.IsActive);
    }

    [Fact]
    public void Controller_NaoExpoeDeleteFisico()
    {
        var deleteMethods = typeof(AdminSegmentsController).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(HttpDeleteAttribute), inherit: false).Any())
            .ToArray();

        Assert.Empty(deleteMethods);
    }

    [Fact]
    public async Task List_IncluiQuantidadeDeCampanhas()
    {
        var segment = Segment("estetica");
        var repo = new Repo([segment], new Dictionary<Guid, int> { [segment.Id] = 3 });
        var controller = new AdminSegmentsController(repo);

        var result = await controller.List(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var item = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AdminSegmentResponse>>(ok.Value));
        Assert.Equal(3, item.CampaignsCount);
    }

    private static Segment Segment(string slug)
    {
        return new Segment
        {
            Id = Guid.NewGuid(),
            Name = slug,
            Slug = slug,
            TemplateKey = "local_service_lead_generation",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class Repo(
        IReadOnlyList<Segment>? initial = null,
        IReadOnlyDictionary<Guid, int>? campaignsCount = null) : ISegmentRepository
    {
        private readonly List<Segment> segments = initial?.ToList() ?? [];
        private readonly IReadOnlyDictionary<Guid, int> campaignsCount = campaignsCount ?? new Dictionary<Guid, int>();

        public IReadOnlyList<Segment> Segments => segments;
        public int SaveCalls { get; private set; }

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
            return Task.FromResult<IReadOnlyList<Segment>>(segments.OrderBy(x => x.Name).ToArray());
        }

        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(segments.Any(x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value)));
        }

        public Task<int> CountCampaignsAsync(Guid segmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(campaignsCount.GetValueOrDefault(segmentId));
        }

        public Task AddAsync(Segment segment, CancellationToken cancellationToken)
        {
            segments.Add(segment);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}
