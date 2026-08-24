using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Tests;

public sealed class ConfiguracoesTests
{
    [Fact]
    public async Task Resolver_PrioridadeBancoSobreAppsettingsEPadrao()
    {
        var repo = new Repo();
        repo.Configs.Add(new ConfiguracaoSistema { Id = Guid.NewGuid(), Chave = "OpenRouter.Model", Categoria = CategoriaConfiguracao.OpenRouter, Valor = "modelo-banco", Ativo = true });
        var resolver = Resolver(repo, new Dictionary<string, string?> { ["OpenRouter:Model"] = "modelo-appsettings" });

        var result = await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", CancellationToken.None);

        Assert.Equal("modelo-banco", result.Value);
        Assert.Equal(OrigemConfiguracao.Banco, result.Origem);
    }

    [Fact]
    public async Task Resolver_FallbackParaAppsettings()
    {
        var resolver = Resolver(new Repo(), new Dictionary<string, string?> { ["OpenRouter:Model"] = "modelo-appsettings" });

        var result = await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", CancellationToken.None);

        Assert.Equal("modelo-appsettings", result.Value);
        Assert.Equal(OrigemConfiguracao.AppSettings, result.Origem);
    }

    [Fact]
    public async Task Segredo_ProtegidoENaoExpostoNaLeitura()
    {
        var repo = new Repo();
        var service = Service(repo);

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["ApiKey"] = "segredo" }, CancellationToken.None);
        var leitura = await service.ObterCategoriaAsync(CategoriaConfiguracao.OpenRouter, CancellationToken.None);

        Assert.NotEqual("segredo", repo.Configs.Single(x => x.Chave == "OpenRouter.ApiKey").ValorProtegido);
        var apiKey = leitura.Configuracoes.Single(x => x.Chave == "ApiKey");
        Assert.Null(apiKey.Valor);
        Assert.True(apiKey.Configurado);
    }

    [Fact]
    public async Task Segredo_NaoEnviadoMantemValorAtual()
    {
        var repo = new Repo();
        var service = Service(repo);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["ApiKey"] = "segredo" }, CancellationToken.None);
        var protectedValue = repo.Configs.Single(x => x.Chave == "OpenRouter.ApiKey").ValorProtegido;

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["Model"] = "modelo" }, CancellationToken.None);

        Assert.Equal(protectedValue, repo.Configs.Single(x => x.Chave == "OpenRouter.ApiKey").ValorProtegido);
    }

    [Fact]
    public async Task OpcionalNaoSensivel_ComDefaultVazio_RemoveOverrideAoReceberVazio()
    {
        var repo = new Repo();
        var resolver = Resolver(repo);
        var service = Service(repo, resolver);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.GoogleAds, new Dictionary<string, object?> { ["LoginCustomerId"] = "1948459907" }, CancellationToken.None);

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.GoogleAds, new Dictionary<string, object?> { ["LoginCustomerId"] = "" }, CancellationToken.None);

        var config = repo.Configs.Single(x => x.Chave == "GoogleAds.LoginCustomerId");
        Assert.False(config.Ativo);
        Assert.Null(config.Valor);
        Assert.Null(config.ValorProtegido);
    }

    [Fact]
    public async Task Resolver_AposLimparLoginCustomerId_NaoRetornaOrigemBanco()
    {
        var repo = new Repo();
        var resolver = Resolver(repo);
        var service = Service(repo, resolver);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.GoogleAds, new Dictionary<string, object?> { ["LoginCustomerId"] = "1948459907" }, CancellationToken.None);
        Assert.Equal(OrigemConfiguracao.Banco, (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "LoginCustomerId", CancellationToken.None)).Origem);

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.GoogleAds, new Dictionary<string, object?> { ["LoginCustomerId"] = "   " }, CancellationToken.None);

        var result = await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "LoginCustomerId", CancellationToken.None);
        Assert.NotEqual(OrigemConfiguracao.Banco, result.Origem);
        Assert.Equal(OrigemConfiguracao.Padrao, result.Origem);
        Assert.False(result.Configured);
        Assert.Equal("", result.Value);
    }

    [Theory]
    [InlineData("DefaultCpcBid", "1.25")]
    [InlineData("TestCustomerId", "1234567890")]
    [InlineData("OptimizationModel", "google-ads-optimizer")]
    public async Task OpcionaisGoogleAds_ComDefaultVazio_SeguemMesmaRegraDeRemocao(string key, string value)
    {
        var repo = new Repo();
        var service = Service(repo);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.GoogleAds, new Dictionary<string, object?> { [key] = value }, CancellationToken.None);

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.GoogleAds, new Dictionary<string, object?> { [key] = "" }, CancellationToken.None);

        var config = repo.Configs.Single(x => x.Chave == $"GoogleAds.{key}");
        Assert.False(config.Ativo);
        Assert.Null(config.Valor);
        Assert.Null(config.ValorProtegido);
    }

    [Fact]
    public async Task Segredo_RemocaoExplicita()
    {
        var repo = new Repo();
        var service = Service(repo);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["ApiKey"] = "segredo" }, CancellationToken.None);

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["removerApiKey"] = true }, CancellationToken.None);

        var config = repo.Configs.Single(x => x.Chave == "OpenRouter.ApiKey");
        Assert.False(config.Ativo);
        Assert.Null(config.ValorProtegido);
    }

    [Fact]
    public async Task Historico_NaoArmazenaSegredo()
    {
        var repo = new Repo();
        await Service(repo).AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["ApiKey"] = "segredo" }, CancellationToken.None);

        var historico = Assert.Single(repo.Historico);
        Assert.True(historico.Sensivel);
        Assert.Null(historico.ValorAnterior);
        Assert.Null(historico.ValorNovo);
    }

    [Fact]
    public async Task Cache_InvalidaAoAtualizar()
    {
        var repo = new Repo();
        var resolver = Resolver(repo);
        var service = Service(repo, resolver);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["Model"] = "modelo-1" }, CancellationToken.None);
        Assert.Equal("modelo-1", (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", CancellationToken.None)).Value);

        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.OpenRouter, new Dictionary<string, object?> { ["Model"] = "modelo-2" }, CancellationToken.None);

        Assert.Equal("modelo-2", (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", CancellationToken.None)).Value);
    }

    [Fact]
    public async Task Validacao_OpenRouterTimeout()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(new Repo()).AtualizarCategoriaAsync(
            CategoriaConfiguracao.OpenRouter,
            new Dictionary<string, object?> { ["TimeoutSeconds"] = 2 },
            CancellationToken.None));
        Assert.Contains("TimeoutSeconds", ex.Message);
    }

    [Fact]
    public async Task CampoObrigatorio_ComDefaultNaoVazio_ContinuaRejeitandoVazio()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(new Repo()).AtualizarCategoriaAsync(
            CategoriaConfiguracao.GoogleAds,
            new Dictionary<string, object?> { ["RedirectUri"] = "" },
            CancellationToken.None));

        Assert.Contains("RedirectUri", ex.Message);
    }

    [Fact]
    public async Task StatusGeral_RetornaPendenciaWhatsApp()
    {
        var status = await Service(new Repo()).ObterStatusAsync(CancellationToken.None);

        Assert.Equal("Pendente", status.WhatsApp.Status);
        Assert.Contains(status.Pendencias, x => x.Contains("WhatsApp"));
    }

    [Fact]
    public async Task TesteWhatsApp_GeraUrlSemEnviar()
    {
        var repo = new Repo();
        var service = Service(repo);
        await service.AtualizarCategoriaAsync(CategoriaConfiguracao.WhatsApp, new Dictionary<string, object?> { ["Numero"] = "5511999999999" }, CancellationToken.None);

        var result = await service.TestarAsync(CategoriaConfiguracao.WhatsApp, CancellationToken.None);

        Assert.True(result.Sucesso);
        Assert.StartsWith("https://wa.me/5511999999999", result.UrlExemplo);
    }

    private static ConfiguracaoService Service(Repo repo, IConfigurationResolver? resolver = null)
    {
        resolver ??= Resolver(repo);
        return new ConfiguracaoService(repo, resolver, new Protector(), new WhatsAppUrlBuilder(Options.Create(new WhatsAppOptions()), resolver));
    }

    private static ConfigurationResolver Resolver(Repo repo, Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build();
        return new ConfigurationResolver(repo, new Protector(), configuration, new MemoryCache(new MemoryCacheOptions()));
    }

    private sealed class Protector : ISecretProtector
    {
        public string Protect(string value) => $"protected:{value}";
        public string Unprotect(string protectedValue) => protectedValue.Replace("protected:", string.Empty);
    }

    private sealed class Repo : IConfiguracaoRepository
    {
        public List<ConfiguracaoSistema> Configs { get; } = [];
        public List<ConfiguracaoSistemaHistorico> Historico { get; } = [];
        public Task<IReadOnlyList<ConfiguracaoSistema>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConfiguracaoSistema>>(Configs);
        public Task<IReadOnlyList<ConfiguracaoSistema>> ListarPorCategoriaAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConfiguracaoSistema>>(Configs.Where(x => x.Categoria == categoria).ToArray());
        public Task<ConfiguracaoSistema?> ObterPorChaveAsync(string chave, CancellationToken cancellationToken) => Task.FromResult(Configs.FirstOrDefault(x => x.Chave == chave));
        public Task AdicionarAsync(ConfiguracaoSistema configuracao, CancellationToken cancellationToken) { Configs.Add(configuracao); return Task.CompletedTask; }
        public Task AdicionarHistoricoAsync(ConfiguracaoSistemaHistorico historico, CancellationToken cancellationToken) { Historico.Add(historico); return Task.CompletedTask; }
        public Task<IReadOnlyList<ConfiguracaoSistemaHistorico>> ListarHistoricoAsync(CategoriaConfiguracao? categoria, string? chave, DateTime? dataInicial, DateTime? dataFinal, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConfiguracaoSistemaHistorico>>(Historico);
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
