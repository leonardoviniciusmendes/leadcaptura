using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class CampanhaServiceTests
{
    [Fact]
    public async Task FakeGeneration_GeraNomeDaCampanha()
    {
        var result = await new FakeCampaignGenerationService().GenerateAsync(BriefingPadrao(), CancellationToken.None);
        Assert.Equal("Plano Familiar Amil - Barra da Tijuca", result.Nome);
    }

    [Fact]
    public async Task FakeGeneration_GeraSlugDaCampanha()
    {
        var result = await new FakeCampaignGenerationService().GenerateAsync(BriefingPadrao(), CancellationToken.None);
        Assert.Equal("plano-familiar-amil-barra-da-tijuca", result.Slug);
    }

    [Fact]
    public async Task GerarCampanha_ValidaOrcamento()
    {
        var service = Service();
        var request = BriefingPadrao() with { OrcamentoDiario = 0 };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GerarCampanhaAsync(request, CancellationToken.None));

        Assert.Contains("Orcamento diario deve ser maior que zero.", exception.Message);
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
    public async Task GerarCampanha_TrataSlugDuplicado()
    {
        var service = Service();

        var primeira = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var segunda = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        Assert.Equal("plano-familiar-amil-barra-da-tijuca", primeira.Slug);
        Assert.Equal("plano-familiar-amil-barra-da-tijuca-2", segunda.Slug);
    }

    [Fact]
    public async Task GerarCampanha_StatusErroQuandoProviderFalha()
    {
        var repository = new InMemoryCampanhaRepository();
        var service = new CampanhaService(repository, new FailingGenerationService());

        await Assert.ThrowsAsync<CampaignGenerationException>(() => service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None));

        var campanha = Assert.Single(repository.Campanhas);
        Assert.Equal(StatusCampanha.Erro, campanha.Status);
        Assert.NotNull(campanha.ErroGeracao);
    }

    [Fact]
    public async Task GerarCampanha_NaoPersisteConteudoParcialQuandoProviderFalha()
    {
        var repository = new InMemoryCampanhaRepository();
        var service = new CampanhaService(repository, new FailingGenerationService());

        await Assert.ThrowsAsync<CampaignGenerationException>(() => service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None));

        var campanha = Assert.Single(repository.Campanhas);
        Assert.Equal(string.Empty, campanha.TituloLandingPage);
        Assert.Equal(string.Empty, campanha.MensagemWhatsApp);
    }

    [Fact]
    public async Task RevisaoManual_AtualizaConteudoEGeraHistorico()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);

        var revisada = await review.RevisarCampanhaAsync(criada.Id, RequestValido(criada) with
        {
            Nome = "Campanha revisada",
            MensagemWhatsApp = "Ola, quero comparar opcoes de plano de saude."
        }, CancellationToken.None);

        Assert.Equal("Campanha revisada", revisada.Nome);
        Assert.Equal(StatusCampanha.Gerada, revisada.Status);
        var historico = Assert.Single(repository.Revisoes);
        Assert.Equal(OrigemRevisaoCampanha.Manual, historico.Origem);
        Assert.DoesNotContain("OPENROUTER", historico.ConteudoNovo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegeneracaoParcial_AtualizaSomenteSecaoEGeraHistoricoIa()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var antes = criada.DescricoesAnuncios[0];
        var review = ReviewService(repository, new StubSectionGenerationService(CampanhaSecao.TitulosAnuncios, TitulosValidos("Familias")));

        var revisada = await review.RegenerarSecaoAsync(criada.Id, new RegenerarCampanhaSecaoRequest(CampanhaSecao.TitulosAnuncios, "foco em familias"), CancellationToken.None);

        Assert.Contains("Familias", revisada.TitulosAnuncios[0]);
        Assert.Equal(antes, revisada.DescricoesAnuncios[0]);
        var historico = Assert.Single(repository.Revisoes);
        Assert.Equal(OrigemRevisaoCampanha.InteligenciaArtificial, historico.Origem);
        Assert.Equal("OpenRouter", historico.ProviderIa);
    }

    [Fact]
    public async Task RegeneracaoParcial_SecaoInvalidaFalha()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => review.RegenerarSecaoAsync(criada.Id, new RegenerarCampanhaSecaoRequest((CampanhaSecao)999, null), CancellationToken.None));
    }

    [Fact]
    public async Task CampanhaInexistente_Falha()
    {
        var review = ReviewService(new InMemoryCampanhaRepository());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => review.AprovarCampanhaAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task OpenRouterComErro_PreservaConteudoAtualENaoCriaHistorico()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository, new FailingSectionGenerationService());

        await Assert.ThrowsAsync<CampaignGenerationException>(() => review.RegenerarSecaoAsync(criada.Id, new RegenerarCampanhaSecaoRequest(CampanhaSecao.Nome, null), CancellationToken.None));

        var atual = await review.ObterRevisaoAsync(criada.Id, CancellationToken.None);
        Assert.Equal(criada.Nome, atual!.Nome);
        Assert.Empty(repository.Revisoes);
    }

    [Fact]
    public async Task AprovacaoValida_AlteraStatusERegistraHistorico()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);

        var aprovada = await review.AprovarCampanhaAsync(criada.Id, CancellationToken.None);

        Assert.Equal(StatusCampanha.Revisada, aprovada.Status);
        Assert.Equal("Aprovacao", Assert.Single(repository.Revisoes).TipoAlteracao);
    }

    [Fact]
    public async Task Aprovacao_ReprovaTitulosForaDoLimite()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        repository.Campanhas[0].TitulosAnunciosJson = System.Text.Json.JsonSerializer.Serialize(TitulosValidos("x").Take(7).ToArray());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).AprovarCampanhaAsync(criada.Id, CancellationToken.None));
        Assert.Contains("Titulos deve conter entre 8 e 12 itens", ex.Message);
    }

    [Fact]
    public async Task Aprovacao_ReprovaDescricaoInvalida()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        repository.Campanhas[0].DescricoesAnunciosJson = System.Text.Json.JsonSerializer.Serialize(new[] { "uma", "duas" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).AprovarCampanhaAsync(criada.Id, CancellationToken.None));
        Assert.Contains("Descricoes deve conter entre 3 e 4 itens", ex.Message);
    }

    [Fact]
    public async Task RevisaoManual_ReprovaPalavrasDuplicadas()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var request = RequestValido(criada) with { PalavrasChave = ["plano saude", "plano saude", "cotacao plano"] };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).RevisarCampanhaAsync(criada.Id, request, CancellationToken.None));
        Assert.Contains("Palavras-chave nao deve conter duplicatas", ex.Message);
    }

    [Fact]
    public async Task RevisaoManual_ReprovaConflitoPalavraPositivaENegativa()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var request = RequestValido(criada) with { PalavrasChaveNegativas = ["emprego", criada.PalavrasChave[0]] };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).RevisarCampanhaAsync(criada.Id, request, CancellationToken.None));
        Assert.Contains("simultaneamente", ex.Message);
    }

    [Fact]
    public async Task AlteracaoAposAprovacao_RetornaStatusGerada()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);
        await review.AprovarCampanhaAsync(criada.Id, CancellationToken.None);

        var editada = await review.RevisarCampanhaAsync(criada.Id, RequestValido(criada) with { Nome = "Reaberta" }, CancellationToken.None);

        Assert.Equal(StatusCampanha.Gerada, editada.Status);
    }

    [Fact]
    public async Task ConsultaHistorico_RetornaSemConteudoSensivel()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);
        await review.AprovarCampanhaAsync(criada.Id, CancellationToken.None);

        var historico = await review.ListarHistoricoAsync(criada.Id, CancellationToken.None);

        var item = Assert.Single(historico);
        Assert.Equal("Aprovacao", item.ResumoAlteracao);
        Assert.Null(item.Provider);
        Assert.Null(item.Modelo);
    }

    public static GerarCampanhaRequest BriefingPadrao()
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

    private static CampanhaService Service(InMemoryCampanhaRepository? repository = null)
    {
        return new CampanhaService(repository ?? new InMemoryCampanhaRepository(), new FakeCampaignGenerationService());
    }

    private static CampaignReviewService ReviewService(InMemoryCampanhaRepository repository, ICampaignSectionGenerationService? generation = null)
    {
        return new CampaignReviewService(repository, generation ?? new StubSectionGenerationService(CampanhaSecao.Nome, "Nome IA"));
    }

    private static RevisarCampanhaRequest RequestValido(CampanhaResponse campanha)
    {
        return new RevisarCampanhaRequest(
            campanha.Nome,
            campanha.TituloLandingPage,
            campanha.SubtituloLandingPage,
            campanha.TextoBotao,
            campanha.MensagemWhatsApp,
            campanha.Beneficios,
            campanha.PerguntasFrequentes,
            campanha.PalavrasChave,
            campanha.PalavrasChaveNegativas,
            campanha.TitulosAnuncios,
            campanha.DescricoesAnuncios);
    }

    private static IReadOnlyList<string> TitulosValidos(string prefixo)
    {
        return Enumerable.Range(1, 8).Select(i => $"{prefixo} {i}").ToArray();
    }

    private sealed class FailingGenerationService : ICampaignGenerationService
    {
        public Task<CampaignGenerationResult> GenerateAsync(GerarCampanhaRequest briefing, CancellationToken cancellationToken)
        {
            throw new CampaignGenerationException("Falha simulada.");
        }
    }

    private sealed class FailingSectionGenerationService : ICampaignSectionGenerationService
    {
        public Task<CampaignSectionGenerationResult> GenerateAsync(Campanha campanha, CampanhaSecao secao, string? instrucaoAdicional, CancellationToken cancellationToken)
        {
            throw new CampaignGenerationException("Falha simulada.");
        }
    }

    private sealed class StubSectionGenerationService(CampanhaSecao secao, object conteudo) : ICampaignSectionGenerationService
    {
        public Task<CampaignSectionGenerationResult> GenerateAsync(Campanha campanha, CampanhaSecao requested, string? instrucaoAdicional, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CampaignSectionGenerationResult(secao, conteudo, "OpenRouter", "test-model"));
        }
    }

    public sealed class InMemoryCampanhaRepository : ICampanhaRepository
    {
        public List<Campanha> Campanhas { get; } = [];
        public List<CampanhaRevisao> Revisoes { get; } = [];

        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
        {
            Campanhas.Add(campanha);
            return Task.CompletedTask;
        }

        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken)
        {
            Revisoes.Add(revisao);
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

        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CampanhaRevisao>>(Revisoes.Where(x => x.CampanhaId == campanhaId).OrderByDescending(x => x.DataAlteracao).ToArray());
        }

        public Task SalvarAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
