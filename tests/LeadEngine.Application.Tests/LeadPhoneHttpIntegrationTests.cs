using System.Net;
using System.Net.Http.Json;
using LeadEngine.Api.Security;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Tests;

public sealed class LeadPhoneHttpIntegrationTests
{
    private const string Email = "admin@leadengine.test";
    private const string Password = "SenhaForte123!";

    [Theory]
    [InlineData("219972390")]
    [InlineData("2133334444")]
    [InlineData("abc21999999999")]
    [InlineData("219999999999")]
    public async Task PostApiLeads_TelefoneInvalido_Retorna400ENaoPersiste(string telefone)
    {
        await using var factory = new LeadPhoneFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LeadEngine.Tests/1.0");
        await LoginAsync(client);

        var response = await client.PostAsJsonAsync("/api/leads", CriarLead(telefone));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("WhatsApp valido com DDD", await response.Content.ReadAsStringAsync());
        Assert.Empty(factory.Leads.Items);
    }

    [Theory]
    [InlineData("21999999999")]
    [InlineData("+55 (21) 99999-9999")]
    public async Task PostApiLeads_TelefoneValido_PersisteNormalizado(string telefone)
    {
        await using var factory = new LeadPhoneFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LeadEngine.Tests/1.0");
        await LoginAsync(client);

        var response = await client.PostAsJsonAsync("/api/leads", CriarLead(telefone));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lead = Assert.Single(factory.Leads.Items);
        Assert.Equal("21999999999", lead.WhatsApp);
        Assert.Equal("21999999999", lead.WhatsAppNormalizado);
    }

    [Theory]
    [InlineData("219972390")]
    [InlineData("2133334444")]
    [InlineData("abc21999999999")]
    [InlineData("219999999999")]
    public async Task PostLandingPublica_TelefoneInvalido_Retorna400ENaoPersiste(string telefone)
    {
        await using var factory = new LeadPhoneFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LeadEngine.Tests/1.0");

        var response = await client.PostAsJsonAsync("/api/publico/campanhas/campanha-teste/leads", CapturarLead(telefone));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("WhatsApp valido com DDD", await response.Content.ReadAsStringAsync());
        Assert.Empty(factory.Leads.Items);
    }

    [Theory]
    [InlineData("21999999999")]
    [InlineData("+55 (21) 99999-9999")]
    public async Task PostLandingPublica_TelefoneValido_PersisteNormalizado(string telefone)
    {
        await using var factory = new LeadPhoneFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LeadEngine.Tests/1.0");

        var response = await client.PostAsJsonAsync("/api/publico/campanhas/campanha-teste/leads", CapturarLead(telefone));

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        var lead = Assert.Single(factory.Leads.Items);
        Assert.Equal("21999999999", lead.WhatsApp);
        Assert.Equal("21999999999", lead.WhatsAppNormalizado);
    }

    private static CriarLeadRequest CriarLead(string telefone) => new()
    {
        Tipo = TipoLead.PessoaFisica,
        Nome = "Maria Silva",
        WhatsApp = telefone,
        QuantidadeVidas = 1,
        ConsentimentoContato = true
    };

    private static CapturarLeadPublicoRequest CapturarLead(string telefone) => new()
    {
        Name = "Maria Silva",
        Phone = telefone,
        FormOpenedAt = DateTimeOffset.UtcNow.AddSeconds(-3).ToUnixTimeMilliseconds()
    };

    private static async Task LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class LeadPhoneFactory : WebApplicationFactory<Program>
    {
        public RecordingLeadRepository Leads { get; } = new();
        private readonly CampaignRepository campaigns = new();

        public LeadPhoneFactory()
        {
            campaigns.Items.Add(new Campanha
            {
                Id = Guid.NewGuid(),
                Nome = "Campanha teste",
                Slug = "campanha-teste",
                Status = StatusCampanha.Revisada,
                Publicada = true,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AdminAuth:Email"] = Email,
                    ["AdminAuth:PasswordHash"] = PasswordHasher.Hash(Password),
                    ["LeadCapture:MinimumFormSeconds"] = "0",
                    ["LeadCapture:MaxLeadsPerIpPerHour"] = "100"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ILeadRepository>();
                services.RemoveAll<ICampanhaRepository>();
                services.RemoveAll<IIntegracaoLeadService>();
                services.RemoveAll<ILeadService>();
                services.RemoveAll<IWhatsAppUrlBuilder>();

                services.AddSingleton<ILeadRepository>(Leads);
                services.AddSingleton<ICampanhaRepository>(campaigns);
                services.AddSingleton<IIntegracaoLeadService, SuccessfulIntegration>();
                services.AddSingleton<IWhatsAppUrlBuilder, WhatsAppBuilder>();
                services.AddScoped<ILeadService>(provider => new LeadService(
                    campaigns,
                    Leads,
                    provider.GetRequiredService<IWhatsAppUrlBuilder>(),
                    provider.GetRequiredService<IRequestContext>(),
                    provider.GetRequiredService<IOptions<LeadCaptureOptions>>()));
            });
        }
    }

    private sealed class CampaignRepository : ICampanhaRepository
    {
        public List<Campanha> Items { get; } = [];
        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) { Items.Add(campanha); return Task.CompletedTask; }
        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(Items.Any(x => x.Slug == slug && x.Id != ignorarId));
        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Slug == slug && x.Publicada && x.Ativo));
        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>([]);
        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>(Items);
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingLeadRepository : ILeadRepository
    {
        public List<Lead> Items { get; } = [];
        public Task<Lead?> ObterDuplicadoRecenteAsync(string whatsAppNormalizado, DateTime criadoApos, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.WhatsAppNormalizado == whatsAppNormalizado && x.CriadoEm >= criadoApos));
        public Task<Lead?> ObterDuplicadoRecenteAsync(Guid campanhaId, string telefoneNormalizado, DateTime criadoApos, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.CampanhaId == campanhaId && x.WhatsAppNormalizado == telefoneNormalizado && x.CriadoEm >= criadoApos));
        public Task<Lead?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<PagedResult<Lead>> ListarAsync(LeadQuery query, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<Lead>(Items, Items.Count, 1, 20));
        public Task<IReadOnlyList<Lead>> ListarPorCampanhaAsync(Guid campanhaId, LeadQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Lead>>(Items.Where(x => x.CampanhaId == campanhaId).ToArray());
        public Task AdicionarAsync(Lead lead, CancellationToken cancellationToken) { Items.Add(lead); return Task.CompletedTask; }
        public Task AdicionarTentativaAsync(TentativaCapturaLead tentativa, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarLogIntegracaoAsync(LogIntegracaoLead log, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SuccessfulIntegration : IIntegracaoLeadService
    {
        public Task<ResultadoIntegracaoLead> EnviarAsync(Lead lead, CancellationToken cancellationToken) => Task.FromResult(new ResultadoIntegracaoLead(true, 200, "OK", "test"));
    }

    private sealed class WhatsAppBuilder : IWhatsAppUrlBuilder
    {
        public string Build(Lead lead, Campanha campanha) => "https://wa.me/5521999999999";
    }
}
