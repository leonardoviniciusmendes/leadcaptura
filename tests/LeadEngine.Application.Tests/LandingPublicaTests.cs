using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Tests;

public sealed class LandingPublicaTests
{
    [Fact]
    public async Task Publicacao_CampanhaRevisada_Publica()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada));

        var result = await new CampaignPublicationService(campanhas).PublicarAsync(campanhas.Campanhas[0].Id, CancellationToken.None);

        Assert.True(result.Publicada);
        Assert.Equal("/lp/plano-familiar-amil-barra", result.UrlPublica);
    }

    [Fact]
    public async Task Publicacao_BloqueiaCampanhaNaoRevisada()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Gerada));

        await Assert.ThrowsAsync<ArgumentException>(() => new CampaignPublicationService(campanhas).PublicarAsync(campanhas.Campanhas[0].Id, CancellationToken.None));
    }

    [Fact]
    public async Task Despublicacao_DesativaLanding()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        var result = await new CampaignPublicationService(campanhas).DespublicarAsync(campanhas.Campanhas[0].Id, CancellationToken.None);

        Assert.False(result.Publicada);
        Assert.False(result.Ativo);
    }

    [Fact]
    public async Task ConsultaPublicaPorSlug_RetornaSomenteDadosPublicos()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        var result = await LeadService(campanhas).ObterCampanhaPublicaAsync("plano-familiar-amil-barra", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Plano Familiar Amil - Barra", result.Nome);
        Assert.Equal("Amil", result.Operadora);
        Assert.True(result.UsesLegacyBriefing);
    }

    [Fact]
    public async Task ConsultaPublica_CampanhaGenerica_RetornaBriefingSemLegacy()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(CampanhaGenerica(publicada: true));

        var result = await LeadService(campanhas).ObterCampanhaPublicaAsync("harmonizacao-facial-barra", CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.UsesLegacyBriefing);
        Assert.Equal("Estetica", result.Segment?.Name);
        Assert.Equal("Harmonizacao facial e tratamentos esteticos", result.Briefing.ProductOrService);
        Assert.Equal("Mulheres da Barra e Recreio", result.Briefing.TargetAudience);
        Assert.Equal("Recreio dos Bandeirantes e Barra da Tijuca", result.Briefing.Location?.Region);
    }

    [Fact]
    public async Task ConsultaPublica_RetornaSchemaFormulario()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(CampanhaGenerica(publicada: true));

        var result = await LeadService(campanhas).ObterCampanhaPublicaAsync("harmonizacao-facial-barra", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Solicitar avaliacao", result.Form.SubmitButtonText);
        Assert.Contains(result.Form.Fields, x => x.Key == "procedimento" && x.Type == "select");
    }

    [Fact]
    public async Task ConsultaPublicaPorSlug_DespublicadaRetornaNulo()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: false));

        var result = await LeadService(campanhas).ObterCampanhaPublicaAsync("plano-familiar-amil-barra", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CapturaLeadValido_PersisteEGeraWhatsApp()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        var result = await LeadService(campanhas, leads).CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.LeadId);
        Assert.Single(leads.Leads);
        Assert.Contains("https://wa.me/5511999999999?text=", result.WhatsAppUrl);
        Assert.Equal("google", leads.Leads[0].UtmSource);
    }

    [Fact]
    public async Task CapturaLead_ConsentimentoNaoObrigatorio()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        var result = await LeadService(campanhas, leads).CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido() with { Consentimento = false }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.LeadId);
        var lead = Assert.Single(leads.Leads);
        Assert.False(lead.ConsentimentoContato);
    }

    [Fact]
    public async Task CapturaLeadGenerico_PersisteLeadAnswerENaoCriaQuantidadeVidasArtificial()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        campanhas.Campanhas.Add(CampanhaGenerica(publicada: true));

        await LeadService(campanhas, leads).CapturarLeadPublicoAsync("harmonizacao-facial-barra", RequestGenerico(), CancellationToken.None);

        var lead = Assert.Single(leads.Leads);
        Assert.Null(lead.QuantidadeVidas);
        Assert.Null(lead.TipoContratacao);
        Assert.Contains(lead.Answers, x => x.FieldKey == "procedimento" && x.LabelSnapshot == "Procedimento de interesse" && x.ValueJson.Contains("Botox"));
        Assert.Contains(lead.Answers, x => x.FieldKey == "preferencias" && x.ValueJson.Contains("Manha"));
        Assert.Contains(lead.Answers, x => x.FieldKey == "aceitaContato" && x.ValueJson == "true");
    }

    [Fact]
    public async Task CapturaLeadGenerico_RequiredFalha()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(CampanhaGenerica(publicada: true));

        var request = RequestGenerico() with { Answers = RequestGenerico().Answers!.Where(x => x.Key != "procedimento").ToDictionary(x => x.Key, x => x.Value) };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => LeadService(campanhas).CapturarLeadPublicoAsync("harmonizacao-facial-barra", request, CancellationToken.None));
        Assert.Contains("Procedimento de interesse obrigatorio.", ex.Message);
    }

    [Fact]
    public async Task CapturaLeadGenerico_SelectInvalidoFalha()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(CampanhaGenerica(publicada: true));
        var answers = RequestGenerico().Answers!.ToDictionary(x => x.Key, x => x.Value);
        answers["procedimento"] = Json("Invalido");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => LeadService(campanhas).CapturarLeadPublicoAsync("harmonizacao-facial-barra", RequestGenerico() with { Answers = answers }, CancellationToken.None));
        Assert.Contains("Procedimento de interesse deve conter uma opcao valida.", ex.Message);
    }

    [Fact]
    public async Task CapturaLeadLegacy_ContinuaPreenchendoColunasAntigas()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        await LeadService(campanhas, leads).CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido(), CancellationToken.None);

        var lead = Assert.Single(leads.Leads);
        Assert.Equal(3, lead.QuantidadeVidas);
        Assert.Equal(TipoContratacaoLead.Familiar, lead.TipoContratacao);
    }

    [Fact]
    public async Task CapturaLead_ValidaTelefone()
    {
        var service = ServiceComCampanhaPublicada();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido() with { Telefone = "123" }, CancellationToken.None));
    }

    [Fact]
    public async Task CapturaLead_NormalizaEstado()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        await LeadService(campanhas, leads).CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido() with { Estado = "rj" }, CancellationToken.None);

        Assert.Equal("RJ", leads.Leads[0].Uf);
    }

    [Fact]
    public async Task CapturaLead_DuplicadoPorTelefoneRetornaSucessoSemNovoLead()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        var campanha = Campanha(StatusCampanha.Revisada, publicada: true);
        campanhas.Campanhas.Add(campanha);
        var service = LeadService(campanhas, leads);

        var primeiro = await service.CapturarLeadPublicoAsync(campanha.Slug, RequestValido(), CancellationToken.None);
        var segundo = await service.CapturarLeadPublicoAsync(campanha.Slug, RequestValido(), CancellationToken.None);

        Assert.Equal(primeiro.LeadId, segundo.LeadId);
        Assert.Single(leads.Leads);
    }

    [Fact]
    public async Task CapturaLead_CampanhaInexistenteFalha()
    {
        var service = LeadService(new CampanhaRepo());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CapturarLeadPublicoAsync("inexistente", RequestValido(), CancellationToken.None));
    }

    [Fact]
    public async Task CapturaLead_CampanhaDespublicadaFalha()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => LeadService(campanhas).CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido(), CancellationToken.None));
    }

    [Fact]
    public void WhatsAppUrl_CodificaMensagem()
    {
        var url = WhatsAppBuilder().Build(new Lead
        {
            Nome = "Maria Silva",
            Cidade = "Rio de Janeiro",
            Uf = "RJ",
            QuantidadeVidas = 3,
            TipoContratacao = TipoContratacaoLead.Familiar,
            Observacao = "Quero cobertura nacional."
        }, Campanha(StatusCampanha.Revisada));

        Assert.Contains("https://wa.me/5511999999999?text=", url);
        Assert.Contains("Maria%20Silva", url);
        Assert.Contains("Rio%20de%20Janeiro%2FRJ", url);
    }

    [Fact]
    public async Task CapturaLead_HoneypotNaoPersiste()
    {
        var campanhas = new CampanhaRepo();
        var leads = new LeadRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));

        var result = await LeadService(campanhas, leads).CapturarLeadPublicoAsync("plano-familiar-amil-barra", RequestValido() with { Website = "bot" }, CancellationToken.None);

        Assert.Equal(Guid.Empty, result.LeadId);
        Assert.Empty(leads.Leads);
    }

    [Fact]
    public void PublicDto_NaoExpoeDadosInternos()
    {
        var names = typeof(CampanhaPublicaResponse).GetProperties().Select(x => x.Name).ToArray();

        Assert.DoesNotContain("ProviderIa", names);
        Assert.DoesNotContain("ModeloIa", names);
        Assert.DoesNotContain("DuracaoGeracaoMs", names);
        Assert.DoesNotContain("ErroGeracao", names);
    }

    [Fact]
    public async Task ListagemLeads_FiltraPorCampanha()
    {
        var repo = new LeadRepo();
        var campanhaId = Guid.NewGuid();
        repo.Leads.Add(new Lead { Id = Guid.NewGuid(), CampanhaId = campanhaId, Nome = "Maria", WhatsApp = "21999999999", WhatsAppNormalizado = "21999999999", CriadoEm = DateTime.UtcNow, Origem = new OrigemLead { Id = Guid.NewGuid() } });
        repo.Leads.Add(new Lead { Id = Guid.NewGuid(), CampanhaId = Guid.NewGuid(), Nome = "Joao", WhatsApp = "21988888888", WhatsAppNormalizado = "21988888888", CriadoEm = DateTime.UtcNow, Origem = new OrigemLead { Id = Guid.NewGuid() } });

        var result = await new LeadConsultaService(repo).ListarAsync(new LeadQuery(campanhaId, null, null, null, null, null, null, null, null, null), CancellationToken.None);

        Assert.Single(result.Itens);
        Assert.Equal("Maria", result.Itens.First().Nome);
    }

    private static LeadService ServiceComCampanhaPublicada()
    {
        var campanhas = new CampanhaRepo();
        campanhas.Campanhas.Add(Campanha(StatusCampanha.Revisada, publicada: true));
        return LeadService(campanhas);
    }

    private static LeadService LeadService(CampanhaRepo campanhas, LeadRepo? leads = null)
    {
        return new LeadService(campanhas, leads ?? new LeadRepo(), WhatsAppBuilder(), new RequestContext(), Options.Create(new LeadCaptureOptions
        {
            ConsentVersion = "1.0",
            MinimumFormSeconds = 2,
            DuplicateWindowHours = 24
        }));
    }

    private static WhatsAppUrlBuilder WhatsAppBuilder()
    {
        return new WhatsAppUrlBuilder(Options.Create(new WhatsAppOptions
        {
            Numero = "5511999999999",
            MensagemPadrao = "Gostaria de receber uma cotacao."
        }));
    }

    private static CapturarLeadPublicoRequest RequestValido()
    {
        return new CapturarLeadPublicoRequest
        {
            Nome = "Maria Silva",
            Telefone = "(21) 99999-9999",
            Email = "maria@email.com",
            Cidade = "Rio de Janeiro",
            Estado = "RJ",
            QuantidadeVidas = 3,
            TipoContratacao = TipoContratacaoLead.Familiar,
            Observacao = "Quero cobertura nacional.",
            Consentimento = true,
            FormOpenedAt = DateTimeOffset.UtcNow.AddSeconds(-3).ToUnixTimeMilliseconds(),
            UtmSource = "google",
            UtmMedium = "cpc",
            UtmCampaign = "familia-rj",
            UtmTerm = "plano familiar",
            UtmContent = "anuncio-1",
            Gclid = "gclid",
            Fbclid = "fbclid"
        };
    }

    private static CapturarLeadPublicoRequest RequestGenerico()
    {
        return new CapturarLeadPublicoRequest
        {
            Name = "Maria Silva",
            Phone = "(21) 99999-9999",
            Email = "maria@email.com",
            FormOpenedAt = DateTimeOffset.UtcNow.AddSeconds(-3).ToUnixTimeMilliseconds(),
            Answers = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["name"] = Json("Maria Silva"),
                ["phone"] = Json("(21) 99999-9999"),
                ["procedimento"] = Json("Botox"),
                ["bairro"] = Json("Recreio"),
                ["preferencias"] = JsonArray("Manha", "Tarde"),
                ["aceitaContato"] = Json(true)
            }
        };
    }

    private static System.Text.Json.JsonElement Json(string value) => System.Text.Json.JsonSerializer.SerializeToElement(value);
    private static System.Text.Json.JsonElement Json(bool value) => System.Text.Json.JsonSerializer.SerializeToElement(value);
    private static System.Text.Json.JsonElement JsonArray(params string[] values) => System.Text.Json.JsonSerializer.SerializeToElement(values);

    private static Campanha Campanha(StatusCampanha status, bool publicada = false)
    {
        var segmentId = Guid.NewGuid();
        return new Campanha
        {
            Id = Guid.NewGuid(),
            Nome = "Plano Familiar Amil - Barra",
            TipoPublico = TipoPublicoCampanha.Familia,
            Cidade = "Rio de Janeiro",
            Estado = "RJ",
            Operadora = "Amil",
            OrcamentoDiario = 20,
            Status = status,
            Slug = "plano-familiar-amil-barra",
            TituloLandingPage = "Plano de saude familiar",
            SubtituloLandingPage = "Compare opcoes conforme seu perfil.",
            TextoBotao = "Falar no WhatsApp",
            MensagemWhatsApp = "Ola, quero comparar opcoes de plano de saude.",
            BeneficiosJson = """["Atendimento consultivo","Comparacao por perfil","Suporte na escolha"]""",
            PerguntasFrequentesJson = """[{"pergunta":"O valor e fixo?","resposta":"Nao. Valores variam por idade, regiao e contratacao."},{"pergunta":"A rede e garantida?","resposta":"Nao. Rede e cobertura dependem do plano."},{"pergunta":"Existe carencia?","resposta":"Carencia depende das regras da operadora."}]""",
            PalavrasChaveJson = """["plano de saude familiar","cotacao plano saude","plano saude rj"]""",
            PalavrasChaveNegativasJson = """["emprego","boleto","login"]""",
            TitulosAnunciosJson = """["Plano Saude RJ","Cotacao Familiar","Fale no WhatsApp","Compare Planos","Atendimento RJ","Plano por Perfil","Consultoria Local","Solicite Cotacao"]""",
            DescricoesAnunciosJson = """["Compare opcoes conforme seu perfil.","Atendimento consultivo para planos de saude.","Solicite contato pelo WhatsApp."]""",
            Publicada = publicada,
            Ativo = publicada,
            UrlPublica = publicada ? "/lp/plano-familiar-amil-barra" : null,
            DataCriacao = DateTime.UtcNow,
            SegmentId = segmentId,
            Segment = new Segment
            {
                Id = segmentId,
                Name = "Planos de Saude",
                Slug = "planos-saude",
                TemplateKey = "high_ticket_quote",
                DefaultConfigJson = """{"legacyCompatibility":true}""",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private static Campanha CampanhaGenerica(bool publicada)
    {
        var segmentId = Guid.NewGuid();
        return new Campanha
        {
            Id = Guid.NewGuid(),
            Nome = "Harmonizacao Facial Barra",
            SegmentId = segmentId,
            Segment = new Segment
            {
                Id = segmentId,
                Name = "Estetica",
                Slug = "estetica",
                TemplateKey = "local_service_lead_generation",
                DefaultConfigJson = """{"ui":{"legacyBriefing":false}}""",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            TipoPublico = TipoPublicoCampanha.Familia,
            Cidade = "Rio de Janeiro",
            Estado = "RJ",
            Regiao = "Recreio dos Bandeirantes e Barra da Tijuca",
            Operadora = "Nenhuma especifica",
            OrcamentoDiario = 40,
            Status = StatusCampanha.Revisada,
            Slug = "harmonizacao-facial-barra",
            TituloLandingPage = "Harmonizacao facial na Barra",
            SubtituloLandingPage = "Agende uma avaliacao estetica personalizada.",
            TextoBotao = "Solicitar avaliacao",
            MensagemWhatsApp = "Ola, quero saber mais sobre harmonizacao facial.",
            BeneficiosJson = """["Avaliacao personalizada","Atendimento local","Orientacao clara"]""",
            PerguntasFrequentesJson = """[{"pergunta":"Como funciona a avaliacao?","resposta":"A equipe retorna com as proximas orientacoes."}]""",
            PalavrasChaveJson = """["harmonizacao facial barra","clinica estetica recreio"]""",
            PalavrasChaveNegativasJson = """[]""",
            TitulosAnunciosJson = """["Harmonizacao Barra"]""",
            DescricoesAnunciosJson = """["Solicite uma avaliacao estetica."]""",
            CampaignConfigJson = """{"productOrService":"Harmonizacao facial e tratamentos esteticos","targetAudience":"Mulheres da Barra e Recreio","campaignGoal":"Agendar avaliacao","offer":"Avaliacao inicial","location":{"city":"Rio de Janeiro","state":"RJ","region":"Recreio dos Bandeirantes e Barra da Tijuca"},"brandTone":"Sofisticado e confiavel"}""",
            Publicada = publicada,
            Ativo = publicada,
            UrlPublica = publicada ? "/lp/harmonizacao-facial-barra" : null,
            DataCriacao = DateTime.UtcNow,
            LeadForms =
            [
                new LeadForm
                {
                    Id = Guid.NewGuid(),
                    Version = 1,
                    SubmitButtonText = "Solicitar avaliacao",
                    IsActive = true,
                    Fields =
                    [
                        new LeadFormField { Id = Guid.NewGuid(), Key = "name", Label = "Nome", Type = "text", Required = true, Order = 1 },
                        new LeadFormField { Id = Guid.NewGuid(), Key = "phone", Label = "WhatsApp", Type = "phone", Required = true, Order = 2 },
                        new LeadFormField { Id = Guid.NewGuid(), Key = "procedimento", Label = "Procedimento de interesse", Type = "select", Required = true, OptionsJson = """["Botox","Preenchimento"]""", Order = 3 },
                        new LeadFormField { Id = Guid.NewGuid(), Key = "bairro", Label = "Bairro", Type = "text", Required = false, Order = 4 },
                        new LeadFormField { Id = Guid.NewGuid(), Key = "preferencias", Label = "Melhores horarios", Type = "multiselect", Required = false, OptionsJson = """["Manha","Tarde","Noite"]""", Order = 5 },
                        new LeadFormField { Id = Guid.NewGuid(), Key = "aceitaContato", Label = "Aceita contato", Type = "checkbox", Required = false, Order = 6 }
                    ]
                }
            ]
        };
    }

    private sealed class RequestContext : IRequestContext
    {
        public string? IpHash => "ip-hash";
        public string? UserAgent => "Mozilla/5.0 Test";
    }

    private sealed class CampanhaRepo : ICampanhaRepository
    {
        public List<Campanha> Campanhas { get; } = [];
        public List<CampanhaRevisao> Revisoes { get; } = [];
        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) { Campanhas.Add(campanha); return Task.CompletedTask; }
        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) { Revisoes.Add(revisao); return Task.CompletedTask; }
        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(Campanhas.Any(x => x.Slug == slug && (ignorarId == null || x.Id != ignorarId)));
        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Campanhas.FirstOrDefault(x => x.Id == id));
        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult(Campanhas.FirstOrDefault(x => x.Slug == slug && x.Publicada && x.Ativo));
        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>(Revisoes.Where(x => x.CampanhaId == campanhaId).ToArray());
        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>(Campanhas.ToArray());
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class LeadRepo : ILeadRepository
    {
        public List<Lead> Leads { get; } = [];
        public Task<Lead?> ObterDuplicadoRecenteAsync(string whatsAppNormalizado, DateTime criadoApos, CancellationToken cancellationToken) => Task.FromResult(Leads.FirstOrDefault(x => x.WhatsAppNormalizado == whatsAppNormalizado && x.CriadoEm >= criadoApos));
        public Task<Lead?> ObterDuplicadoRecenteAsync(Guid campanhaId, string telefoneNormalizado, DateTime criadoApos, CancellationToken cancellationToken) => Task.FromResult(Leads.FirstOrDefault(x => x.CampanhaId == campanhaId && x.WhatsAppNormalizado == telefoneNormalizado && x.CriadoEm >= criadoApos));
        public Task<Lead?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Leads.FirstOrDefault(x => x.Id == id));
        public Task<PagedResult<Lead>> ListarAsync(LeadQuery query, CancellationToken cancellationToken)
        {
            var items = Leads.AsEnumerable();
            if (query.CampanhaId is not null) items = items.Where(x => x.CampanhaId == query.CampanhaId.Value);
            if (query.TipoContratacao is not null) items = items.Where(x => x.TipoContratacao == query.TipoContratacao.Value);
            if (!string.IsNullOrWhiteSpace(query.Origem)) items = items.Where(x => x.OrigemCaptura == query.Origem);
            var array = items.OrderByDescending(x => x.CriadoEm).ToArray();
            return Task.FromResult(new PagedResult<Lead>(array, array.Length, 1, 20));
        }
        public Task<IReadOnlyList<Lead>> ListarPorCampanhaAsync(Guid campanhaId, LeadQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Lead>>(Leads.Where(x => x.CampanhaId == campanhaId).ToArray());
        public Task AdicionarAsync(Lead lead, CancellationToken cancellationToken) { Leads.Add(lead); return Task.CompletedTask; }
        public Task AdicionarTentativaAsync(TentativaCapturaLead tentativa, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarLogIntegracaoAsync(LogIntegracaoLead log, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
