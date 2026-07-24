using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class CampanhaServiceTests
{
    [Fact]
    public void FakeGeneration_GeraNomeDaCampanha()
    {
        var service = new FakeCampaignGenerationService();

        var result = service.Generate(BriefingPadrao());

        Assert.Equal("Plano Familiar Amil - Barra da Tijuca", result.Nome);
    }

    [Fact]
    public void FakeGeneration_GeraSlugDaCampanha()
    {
        var service = new FakeCampaignGenerationService();

        var result = service.Generate(BriefingPadrao());

        Assert.Equal("plano-familiar-amil-barra-da-tijuca", result.Slug);
    }

    [Fact]
    public async Task GerarCampanha_ValidaOrcamento()
    {
        var service = Service();
        var request = BriefingPadrao() with { OrcamentoDiario = 0 };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GerarCampanhaAsync(request, CancellationToken.None));

        Assert.Contains("Orçamento diário deve ser maior que zero.", exception.Message);
    }

    [Fact]
    public async Task GerarCampanha_CriaCampanha()
    {
        var repository = new InMemoryCampanhaRepository();
        var service = Service(repository);

        var campanha = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, campanha.Id);
        Assert.Equal(StatusCampanha.Gerada, campanha.Status);
        Assert.Single(repository.Campanhas);
    }

    [Fact]
    public async Task ObterCampanhaPorId_RetornaCampanha()
    {
        var service = Service();
        var criada = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        var encontrada = await service.ObterCampanhaPorIdAsync(criada.Id, CancellationToken.None);

        Assert.NotNull(encontrada);
        Assert.Equal(criada.Id, encontrada.Id);
    }

    [Fact]
    public async Task RevisarCampanha_AtualizaConteudo()
    {
        var service = Service();
        var criada = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        var revisada = await service.RevisarCampanhaAsync(criada.Id, new RevisarCampanhaRequest(
            "Campanha revisada",
            criada.TituloLandingPage,
            criada.SubtituloLandingPage,
            criada.TextoBotao,
            "Mensagem revisada",
            criada.Slug,
            criada.Objetivo,
            StatusCampanha.Revisada), CancellationToken.None);

        Assert.Equal("Campanha revisada", revisada.Nome);
        Assert.Equal("Mensagem revisada", revisada.MensagemWhatsApp);
        Assert.Equal(StatusCampanha.Revisada, revisada.Status);
        Assert.NotNull(revisada.DataAtualizacao);
    }

    [Fact]
    public async Task GerarCampanha_TrataSlugDuplicado()
    {
        var service = Service();

        var primeira = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var segunda = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        Assert.Equal("plano-familiar-amil-barra-da-tijuca", primeira.Slug);
        Assert.Equal("plano-familiar-amil-barra-da-tijuca-2", segunda.Slug);
    }

    private static CampanhaService Service(InMemoryCampanhaRepository? repository = null)
    {
        return new CampanhaService(repository ?? new InMemoryCampanhaRepository(), new FakeCampaignGenerationService());
    }

    private static GerarCampanhaRequest BriefingPadrao()
    {
        return new GerarCampanhaRequest(
            TipoPublicoCampanha.Familia,
            "Rio de Janeiro",
            "RJ",
            "Barra da Tijuca",
            "Amil",
            null,
            20,
            null);
    }

    private sealed class InMemoryCampanhaRepository : ICampanhaRepository
    {
        public List<Campanha> Campanhas { get; } = [];

        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
        {
            Campanhas.Add(campanha);
            return Task.CompletedTask;
        }

        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Campanhas.Any(x => x.Slug == slug && (ignorarId == null || x.Id != ignorarId)));
        }

        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Campanha>>(Campanhas.OrderByDescending(x => x.DataCriacao).ToArray());
        }

        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Campanhas.FirstOrDefault(x => x.Id == id));
        }

        public Task SalvarAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
