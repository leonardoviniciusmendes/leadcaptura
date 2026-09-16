using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class BrazilianMobilePhoneTests
{
    [Theory]
    [InlineData("(21) 99999-9999", "21999999999")]
    [InlineData("+55 (21) 99999-9999", "21999999999")]
    [InlineData("5521999999999", "21999999999")]
    [InlineData("21999999999", "21999999999")]
    [InlineData(" (21) 99999-9999 ", "21999999999")]
    public void Normalizador_AceitaCelularBrasileiro(string input, string esperado)
    {
        var valido = LeadSanitizer.TryNormalizarCelularBrasileiro(input, out var normalizado);

        Assert.True(valido);
        Assert.Equal(esperado, normalizado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("219972390")]
    [InlineData("219982174")]
    [InlineData("999999999")]
    [InlineData("2133334444")]
    [InlineData("telefone 21999999999")]
    [InlineData("55")]
    [InlineData("21899999999")]
    [InlineData("219999999999")]
    [InlineData("55219999999999")]
    [InlineData("+55 (21) 99999-99999")]
    public void Normalizador_RejeitaTelefoneInvalido(string input)
    {
        var valido = LeadSanitizer.TryNormalizarCelularBrasileiro(input, out var normalizado);

        Assert.False(valido);
        Assert.Equal(string.Empty, normalizado);
    }

    [Fact]
    public void Normalizador_RejeitaNull()
    {
        Assert.False(LeadSanitizer.TryNormalizarCelularBrasileiro(null, out var normalizado));
        Assert.Equal(string.Empty, normalizado);
    }

    [Theory]
    [InlineData("219972390")]
    [InlineData("219982174")]
    [InlineData("999999999")]
    [InlineData("2133334444")]
    [InlineData("telefone 21999999999")]
    [InlineData("55")]
    [InlineData("21899999999")]
    [InlineData("219999999999")]
    [InlineData("55219999999999")]
    [InlineData("+55 (21) 99999-99999")]
    public void LeadValidator_RejeitaCelularInvalido(string telefone)
    {
        var erros = LeadValidator.Validar(RequestValido(telefone));

        Assert.Contains(LeadSanitizer.CelularBrasileiroInvalido, erros);
    }

    [Fact]
    public void LeadValidator_AceitaENormalizadorRemoveCodigoPais()
    {
        var request = RequestValido("+55 (21) 99999-9999");

        Assert.Empty(LeadValidator.Validar(request));
        Assert.True(LeadSanitizer.TryNormalizarCelularBrasileiro(request.WhatsApp, out var normalizado));
        Assert.Equal("21999999999", normalizado);
    }

    [Theory]
    [InlineData("219972390")]
    [InlineData("2133334444")]
    public async Task FluxoApiLeads_NaoPersisteTelefoneInvalido(string telefone)
    {
        var repository = new LeadRepo();
        var service = new LeadCaptureService(repository, new IntegracaoLead(), new RequestContext());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CapturarAsync(RequestValido(telefone), CancellationToken.None));
        Assert.Empty(repository.Leads);
    }

    [Fact]
    public async Task FluxoApiLeads_PersisteTelefoneNormalizadoSemCodigoPais()
    {
        var repository = new LeadRepo();
        var service = new LeadCaptureService(repository, new IntegracaoLead(), new RequestContext());

        await service.CapturarAsync(RequestValido("+55 (21) 99999-9999"), CancellationToken.None);

        var lead = Assert.Single(repository.Leads);
        Assert.Equal("21999999999", lead.WhatsApp);
        Assert.Equal("21999999999", lead.WhatsAppNormalizado);
    }

    private static CriarLeadRequest RequestValido(string telefone) => new()
    {
        Tipo = TipoLead.PessoaFisica,
        Nome = "Maria Silva",
        WhatsApp = telefone,
        QuantidadeVidas = 1,
        ConsentimentoContato = true
    };

    private sealed class IntegracaoLead : IIntegracaoLeadService
    {
        public Task<ResultadoIntegracaoLead> EnviarAsync(Lead lead, CancellationToken cancellationToken) =>
            Task.FromResult(new ResultadoIntegracaoLead(true, 200, "OK", "teste"));
    }

    private sealed class RequestContext : IRequestContext
    {
        public string? IpHash => "ip";
        public string? UserAgent => "test";
        public string? User => "test";
    }

    private sealed class LeadRepo : ILeadRepository
    {
        public List<Lead> Leads { get; } = [];
        public Task<Lead?> ObterDuplicadoRecenteAsync(string whatsAppNormalizado, DateTime criadoApos, CancellationToken cancellationToken) => Task.FromResult<Lead?>(null);
        public Task<Lead?> ObterDuplicadoRecenteAsync(Guid campanhaId, string telefoneNormalizado, DateTime criadoApos, CancellationToken cancellationToken) => Task.FromResult<Lead?>(null);
        public Task<Lead?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Leads.FirstOrDefault(x => x.Id == id));
        public Task<PagedResult<Lead>> ListarAsync(LeadQuery query, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<Lead>(Leads, Leads.Count, 1, 20));
        public Task<IReadOnlyList<Lead>> ListarPorCampanhaAsync(Guid campanhaId, LeadQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Lead>>([]);
        public Task AdicionarAsync(Lead lead, CancellationToken cancellationToken) { Leads.Add(lead); return Task.CompletedTask; }
        public Task AdicionarTentativaAsync(TentativaCapturaLead tentativa, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarLogIntegracaoAsync(LogIntegracaoLead log, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
