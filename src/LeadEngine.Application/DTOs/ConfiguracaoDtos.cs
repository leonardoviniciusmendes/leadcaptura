using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public enum OrigemConfiguracao
{
    Banco = 1,
    VariavelAmbiente = 2,
    AppSettings = 3,
    Padrao = 4
}

public sealed record ConfiguracaoCategoriaResponse(
    CategoriaConfiguracao Categoria,
    IReadOnlyList<ConfiguracaoItemResponse> Configuracoes);

public sealed record ConfiguracaoItemResponse(
    string Chave,
    string? Valor,
    bool Sensivel,
    bool Configurado,
    OrigemConfiguracao Origem,
    string? Descricao);

public sealed record AtualizarConfiguracaoCategoriaRequest(Dictionary<string, object?> Valores);

public sealed record TesteConfiguracaoResponse(bool Sucesso, string Status, string? Modelo = null, long? DuracaoMs = null, string? UrlExemplo = null);

public sealed record ConfiguracoesStatusResponse(
    ConfiguracaoStatusItem OpenRouter,
    ConfiguracaoStatusItem GeracaoIa,
    ConfiguracaoStatusItem WhatsApp,
    ConfiguracaoStatusItem CapturaLeads,
    ConfiguracaoStatusItem ExternalLeadApi,
    ConfiguracaoStatusItem UrlPublica,
    ConfiguracaoStatusItem GoogleAds,
    IReadOnlyList<string> Pendencias);

public sealed record ConfiguracaoStatusItem(bool Configurado, string Status);

public sealed record ConfiguracaoHistoricoQuery(CategoriaConfiguracao? Categoria, string? Chave, DateTime? DataInicial, DateTime? DataFinal);

public sealed record ConfiguracaoHistoricoResponse(
    DateTime DataAlteracao,
    CategoriaConfiguracao Categoria,
    string Chave,
    string? ValorAnterior,
    string? ValorNovo,
    bool Sensivel,
    string OrigemAlteracao);
