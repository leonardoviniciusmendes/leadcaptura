using System.Net;
using System.Net.Http.Json;
using LeadEngine.Api.Security;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LeadEngine.Application.Tests;

public sealed class AuthIntegrationTests
{
    private const string Email = "admin@leadengine.test";
    private const string Password = "SenhaForte123!";

    [Fact]
    public async Task LandingPublicaContinuaAnonima()
    {
        await using var factory = new AuthFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/publico/campanhas/plano-saude-familia-zona-sul-rj");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CapturaLeadPublicaContinuaAnonima()
    {
        await using var factory = new AuthFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/publico/campanhas/plano-saude-familia-zona-sul-rj/leads", new CapturarLeadPublicoRequest
        {
            Nome = "Lead Teste",
            Telefone = "21999999999",
            Cidade = "Rio de Janeiro",
            Estado = "RJ",
            QuantidadeVidas = 3,
            TipoContratacao = TipoContratacaoLead.Familiar,
            Consentimento = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointAdministrativoAnonimoRetorna401()
    {
        await using var factory = new AuthFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/configuracoes/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginValidoPermiteEndpointAdministrativo()
    {
        await using var factory = new AuthFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });
        var admin = await client.GetAsync("/api/configuracoes/status");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task LoginInvalidoRejeitaSemDetalharUsuario()
    {
        await using var factory = new AuthFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = "errada" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Credenciais invalidas", body);
        Assert.DoesNotContain(Email, body);
        Assert.DoesNotContain("SenhaForte", body);
    }

    private sealed class AuthFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AdminAuth:Email"] = Email,
                    ["AdminAuth:PasswordHash"] = PasswordHasher.Hash(Password)
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ILeadService>();
                services.RemoveAll<IConfiguracaoService>();
                services.AddSingleton<ILeadService, PublicLeadStub>();
                services.AddSingleton<IConfiguracaoService, ConfiguracaoStub>();
            });
        }
    }

    private sealed class PublicLeadStub : ILeadService
    {
        public Task<CampanhaPublicaResponse?> ObterCampanhaPublicaAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult<CampanhaPublicaResponse?>(new CampanhaPublicaResponse(
                "Plano Saude Familia Zona Sul RJ",
                "Plano de saude familiar",
                "Cotacao para Zona Sul RJ",
                "Receber cotacao",
                ["Atendimento consultivo"],
                [],
                "Operadora",
                "Rio de Janeiro",
                "RJ",
                TipoPublicoCampanha.Familia,
                "Gostaria de receber uma cotacao."));
        }

        public Task<CapturarLeadPublicoResponse> CapturarLeadPublicoAsync(string slug, CapturarLeadPublicoRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CapturarLeadPublicoResponse(Guid.NewGuid(), "Lead recebido.", "https://wa.me/5521999999999"));
        }
    }

    private sealed class ConfiguracaoStub : IConfiguracaoService
    {
        public Task<IReadOnlyList<ConfiguracaoCategoriaResponse>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConfiguracaoCategoriaResponse>>([]);
        public Task<ConfiguracaoCategoriaResponse> ObterCategoriaAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.FromResult(new ConfiguracaoCategoriaResponse(categoria, []));
        public Task<ConfiguracaoCategoriaResponse> AtualizarCategoriaAsync(CategoriaConfiguracao categoria, Dictionary<string, object?> valores, CancellationToken cancellationToken) => Task.FromResult(new ConfiguracaoCategoriaResponse(categoria, []));
        public Task<TesteConfiguracaoResponse> TestarAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.FromResult(new TesteConfiguracaoResponse(true, "OK"));
        public Task<ConfiguracoesStatusResponse> ObterStatusAsync(CancellationToken cancellationToken) => Task.FromResult(new ConfiguracoesStatusResponse(
            new ConfiguracaoStatusItem(true, "OK"),
            new ConfiguracaoStatusItem(true, "OK"),
            new ConfiguracaoStatusItem(true, "OK"),
            new ConfiguracaoStatusItem(true, "OK"),
            new ConfiguracaoStatusItem(true, "OK"),
            new ConfiguracaoStatusItem(true, "OK"),
            new ConfiguracaoStatusItem(true, "OK"),
            new MetaAdsStatusResponse(true, false, false, "OK"),
            []));
        public Task<IReadOnlyList<ConfiguracaoHistoricoResponse>> ListarHistoricoAsync(ConfiguracaoHistoricoQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConfiguracaoHistoricoResponse>>([]);
    }
}
