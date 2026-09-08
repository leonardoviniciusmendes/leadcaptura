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
        var result = await new FakeCampaignGenerationService().GenerateAsync(ContextoSaude(), CancellationToken.None);
        Assert.Equal("Plano Familiar Amil - Barra da Tijuca", result.Nome);
    }

    [Fact]
    public async Task FakeGeneration_GeraSlugDaCampanha()
    {
        var result = await new FakeCampaignGenerationService().GenerateAsync(ContextoSaude(), CancellationToken.None);
        Assert.Contains("barra-da-tijuca", result.Slug);
    }

    [Fact]
    public async Task FakeGeneration_RespeitaContextoGenerico()
    {
        var request = BriefingPadrao() with
        {
            BusinessDescription = "Oficina local",
            ProductOrService = "Revisao automotiva",
            TargetAudience = "Motoristas",
            CampaignGoal = "Agendar avaliacao",
            Offer = "Checklist inicial",
            Operadora = string.Empty,
            Location = new CampaignLocationDto("Campinas", "SP", "Cambuí")
        };
        var context = CampaignGenerationContextFactory.FromRequest(
            request,
            new Segment { Name = "Oficina mecanica", Slug = "oficina", TemplateKey = "appointment_booking", IsActive = true },
            null);

        var result = await new FakeCampaignGenerationService().GenerateAsync(context, CancellationToken.None);

        Assert.Contains("Revisao automotiva", result.Nome);
        Assert.Contains("Solicitar agendamento", result.TextoBotao);
        Assert.DoesNotContain("plano", result.Nome, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("saude", result.TituloLandingPage, StringComparison.OrdinalIgnoreCase);
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
        var segments = SegmentosPadrao();
        var service = Service(repository);

        var campanha = await service.GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, campanha.Id);
        Assert.Equal(segments.PlanosSaude.Id, campanha.SegmentId);
        Assert.Equal("planos-saude", campanha.SegmentSlug);
        Assert.Equal(StatusCampanha.Gerada, campanha.Status);
        Assert.Single(repository.Campanhas);
    }

    [Fact]
    public async Task GerarCampanha_RequestSemSegmentSlugUsaPlanosSaude()
    {
        var repository = new InMemoryCampanhaRepository();
        var segments = SegmentosPadrao();
        var service = Service(repository, segments);

        var campanha = await service.GerarCampanhaAsync(BriefingPadrao() with { SegmentSlug = null }, CancellationToken.None);

        Assert.Equal(segments.PlanosSaude.Id, campanha.SegmentId);
        Assert.Equal("planos-saude", campanha.SegmentSlug);
        Assert.Equal("Amil", campanha.Operadora);
        Assert.Null(campanha.CampaignConfigJson);
    }

    [Fact]
    public async Task GerarCampanha_RequestComSlugValidoPersisteSegmentIdCorreto()
    {
        var repository = new InMemoryCampanhaRepository();
        var segments = SegmentosPadrao();
        var service = Service(repository, segments);

        var campanha = await service.GerarCampanhaAsync(BriefingPadrao() with { SegmentSlug = "servicos-locais" }, CancellationToken.None);

        Assert.Equal(segments.ServicosLocais.Id, campanha.SegmentId);
        Assert.Equal("servicos-locais", campanha.SegmentSlug);
        Assert.Equal(segments.ServicosLocais.Id, repository.Campanhas[0].SegmentId);
    }

    [Fact]
    public async Task GerarCampanha_SlugInexistenteRetornaErroControlado()
    {
        var service = Service(segments: SegmentosPadrao());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GerarCampanhaAsync(BriefingPadrao() with { SegmentSlug = "segmento-inexistente" }, CancellationToken.None));

        Assert.Contains("Segmento 'segmento-inexistente' nao encontrado ou inativo.", exception.Message);
    }

    [Fact]
    public async Task GerarCampanha_CampaignConfigJsonPersisteContextoGenericoSemQuebrarCamposAntigos()
    {
        var repository = new InMemoryCampanhaRepository();
        var service = Service(repository, SegmentosPadrao());
        var request = BriefingPadrao() with
        {
            SegmentSlug = "servicos-locais",
            BusinessDescription = "Clinica local",
            ProductOrService = "Avaliacao estetica",
            TargetAudience = "Mulheres de 25 a 55 anos",
            CampaignGoal = "Agendar avaliacao",
            Offer = "Primeiro atendimento consultivo",
            BrandTone = "Profissional",
            Location = new CampaignLocationDto("Rio de Janeiro", "rj", "Barra"),
            Restrictions = ["nao prometer resultado", "nao prometer resultado"]
        };

        var campanha = await service.GerarCampanhaAsync(request, CancellationToken.None);

        Assert.Equal("Amil", campanha.Operadora);
        Assert.Equal(TipoPublicoCampanha.Familia, campanha.TipoPublico);
        Assert.NotNull(campanha.CampaignConfigJson);
        Assert.Contains("\"productOrService\":\"Avaliacao estetica\"", campanha.CampaignConfigJson);
        Assert.Contains("\"state\":\"RJ\"", campanha.CampaignConfigJson);
        Assert.Single(System.Text.Json.JsonDocument.Parse(campanha.CampaignConfigJson!).RootElement.GetProperty("restrictions").EnumerateArray());
        Assert.False(campanha.UsesLegacyBriefing);
        Assert.Equal("Avaliacao estetica", campanha.Briefing.ProductOrService);
        Assert.Equal("Mulheres de 25 a 55 anos", campanha.Briefing.TargetAudience);
    }

    [Fact]
    public async Task GerarCampanha_GenericaFuncionaSemOperadora()
    {
        var repository = new InMemoryCampanhaRepository();
        var service = Service(repository, SegmentosPadrao());
        var request = BriefingPadrao() with
        {
            SegmentSlug = "servicos-locais",
            Operadora = string.Empty,
            ProductOrService = "Revisao automotiva",
            TargetAudience = "Motoristas da regiao",
            CampaignGoal = "Agendar avaliacao",
            Location = new CampaignLocationDto("Niteroi", "RJ", null)
        };

        var campanha = await service.GerarCampanhaAsync(request, CancellationToken.None);

        Assert.Equal("Nao se aplica", campanha.Operadora);
        Assert.Equal("servicos-locais", campanha.SegmentSlug);
        Assert.Equal("Niteroi", campanha.Cidade);
        Assert.Equal("RJ", campanha.Estado);
    }

    [Fact]
    public async Task GerarCampanha_LegacyContinuaFuncionando()
    {
        var campanha = await Service(segments: SegmentosPadrao()).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);

        Assert.Equal("Plano Familiar Amil - Barra da Tijuca", campanha.Nome);
        Assert.Equal("Amil", campanha.Operadora);
        Assert.Equal("planos-saude", campanha.SegmentSlug);
        Assert.True(campanha.UsesLegacyBriefing);
    }

    [Fact]
    public async Task SegmentRepository_ListActiveRetornaSomenteAtivos()
    {
        var segments = SegmentosPadrao();

        var result = await segments.ListActiveAsync(CancellationToken.None);

        Assert.Contains(result, x => x.Slug == "planos-saude");
        Assert.Contains(result, x => x.Slug == "servicos-locais");
        Assert.DoesNotContain(result, x => x.Slug == "inativo");
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
    public async Task RevisaoManual_AtualizaInformacoesGeraisDoBriefing()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);

        var revisada = await review.RevisarCampanhaAsync(criada.Id, RequestValido(criada) with
        {
            Nome = "Campanha estetica",
            ProductOrService = "Harmonizacao facial",
            TargetAudience = "Mulheres de 25 a 55 anos",
            CampaignGoal = "Gerar agendamentos de avaliacao",
            Offer = "Avaliacao inicial",
            BrandTone = "Profissional e acolhedor",
            Location = new CampaignLocationDto("Niteroi", "rj", "Icarai"),
            OrcamentoDiario = 55.50m
        }, CancellationToken.None);

        Assert.Equal("Campanha estetica", revisada.Nome);
        Assert.Equal("Harmonizacao facial", revisada.Briefing.ProductOrService);
        Assert.Equal("Mulheres de 25 a 55 anos", revisada.Briefing.TargetAudience);
        Assert.Equal("Gerar agendamentos de avaliacao", revisada.Briefing.CampaignGoal);
        Assert.Equal("Avaliacao inicial", revisada.Briefing.Offer);
        Assert.Equal("Profissional e acolhedor", revisada.Briefing.BrandTone);
        Assert.Equal("Niteroi", revisada.Cidade);
        Assert.Equal("RJ", revisada.Estado);
        Assert.Equal("Icarai", revisada.Regiao);
        Assert.Equal(55.50m, revisada.OrcamentoDiario);
        Assert.Contains("\"productOrService\":\"Harmonizacao facial\"", repository.Campanhas[0].CampaignConfigJson);
    }

    [Fact]
    public async Task RevisaoManual_ReprovaOrcamentoInvalido()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var request = RequestValido(criada) with { OrcamentoDiario = 0 };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).RevisarCampanhaAsync(criada.Id, request, CancellationToken.None));

        Assert.Contains("Orcamento diario deve ser maior que zero", ex.Message);
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
        await Assert.ThrowsAsync<KeyNotFoundException>(() => review.AprovarCampanhaAsync(Guid.NewGuid(), new AprovarCampanhaRequest(), CancellationToken.None));
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

        var aprovada = (await review.AprovarCampanhaAsync(criada.Id, new AprovarCampanhaRequest(), CancellationToken.None)).Campanha;

        Assert.Equal(StatusCampanha.Revisada, aprovada.Status);
        Assert.Equal("Aprovacao", Assert.Single(repository.Revisoes).TipoAlteracao);
    }

    [Fact]
    public async Task Aprovacao_ReprovaTitulosForaDoLimite()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        repository.Campanhas[0].TitulosAnunciosJson = System.Text.Json.JsonSerializer.Serialize(TitulosValidos("x").Take(7).ToArray());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).AprovarCampanhaAsync(criada.Id, new AprovarCampanhaRequest(), CancellationToken.None));
        Assert.Contains("Titulos deve conter entre 8 e 12 itens", ex.Message);
    }

    [Fact]
    public async Task Aprovacao_ReprovaDescricaoInvalida()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        repository.Campanhas[0].DescricoesAnunciosJson = System.Text.Json.JsonSerializer.Serialize(new[] { "uma", "duas" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => ReviewService(repository).AprovarCampanhaAsync(criada.Id, new AprovarCampanhaRequest(), CancellationToken.None));
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
        await review.AprovarCampanhaAsync(criada.Id, new AprovarCampanhaRequest(), CancellationToken.None);

        var editada = await review.RevisarCampanhaAsync(criada.Id, RequestValido(criada) with { Nome = "Reaberta" }, CancellationToken.None);

        Assert.Equal(StatusCampanha.Gerada, editada.Status);
    }

    [Fact]
    public async Task ConsultaHistorico_RetornaSemConteudoSensivel()
    {
        var repository = new InMemoryCampanhaRepository();
        var criada = await Service(repository).GerarCampanhaAsync(BriefingPadrao(), CancellationToken.None);
        var review = ReviewService(repository);
        await review.AprovarCampanhaAsync(criada.Id, new AprovarCampanhaRequest(), CancellationToken.None);

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

    private static CampanhaService Service(InMemoryCampanhaRepository? repository = null, InMemorySegmentRepository? segments = null)
    {
        return new CampanhaService(repository ?? new InMemoryCampanhaRepository(), new FakeCampaignGenerationService(), segments ?? SegmentosPadrao());
    }

    private static InMemorySegmentRepository SegmentosPadrao()
    {
        return new InMemorySegmentRepository([
            new Segment { Id = Guid.Parse("3f1ce0a4-7ec5-4c8f-b6d9-df4f3e7f0c35"), Name = "Planos de Saude", Slug = "planos-saude", TemplateKey = "high_ticket_quote", DefaultConfigJson = """{"legacyCompatibility":true}""", IsActive = true },
            new Segment { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Servicos Locais", Slug = "servicos-locais", TemplateKey = "local_service_lead_generation", IsActive = true },
            new Segment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Inativo", Slug = "inativo", TemplateKey = "local_service_lead_generation", IsActive = false }
        ]);
    }

    private static CampaignReviewService ReviewService(InMemoryCampanhaRepository repository, ICampaignSectionGenerationService? generation = null)
    {
        var assets = new InMemoryCreativeAssetRepository();
        return new CampaignReviewService(
            repository,
            generation ?? new StubSectionGenerationService(CampanhaSecao.Nome, "Nome IA"),
            new CreativeQualityGateService(assets),
            new InMemoryCreativeQualityOverrideRepository(),
            new RequestContext());
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
        public Task<CampaignGenerationResult> GenerateAsync(CampaignGenerationContext context, CancellationToken cancellationToken)
        {
            throw new CampaignGenerationException("Falha simulada.");
        }
    }

    private static CampaignGenerationContext ContextoSaude()
    {
        var briefing = BriefingPadrao();
        var segment = SegmentosPadrao().PlanosSaude;
        return CampaignGenerationContextFactory.FromRequest(briefing, segment, null);
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

        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(Campanhas.FirstOrDefault(x => x.Slug == slug && x.Publicada && x.Ativo));
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

    private sealed class InMemoryCreativeAssetRepository : ICreativeAssetRepository
    {
        public Task<CreativeAsset?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<CreativeAsset?>(null);
        public Task<IReadOnlyList<CreativeAsset>> ListarPorCampanhaAsync(Guid campaignId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CreativeAsset>>([]);
        public Task AdicionarAsync(CreativeAsset asset, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarAnaliseAsync(CreativeAssetAnalysis analysis, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Remover(CreativeAsset asset) { }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryCreativeQualityOverrideRepository : ICreativeQualityOverrideRepository
    {
        public Task AdicionarAsync(CreativeQualityOverride item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RequestContext : IRequestContext
    {
        public string? IpHash => "ip";
        public string? UserAgent => "test";
        public string? User => "tester";
    }

    private sealed class InMemorySegmentRepository(IReadOnlyList<Segment> segments) : ISegmentRepository
    {
        public Segment PlanosSaude => segments.Single(x => x.Slug == "planos-saude");
        public Segment ServicosLocais => segments.Single(x => x.Slug == "servicos-locais");

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
            return Task.FromResult<IReadOnlyList<Segment>>(segments.Where(x => x.IsActive).OrderBy(x => x.Name).ToArray());
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
