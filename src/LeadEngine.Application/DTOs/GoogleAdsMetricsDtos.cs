using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record GoogleAdsPeriodoRequest(DateOnly? DataInicial, DateOnly? DataFinal);
public sealed record GoogleAdsStatusActionRequest(bool ConfirmarAtivacaoEmContaTeste = false);
public sealed record GoogleAdsAtualizacaoRequest(decimal? OrcamentoDiario, IReadOnlyList<string>? Headlines, IReadOnlyList<string>? Descriptions, string? Status, bool ConfirmarAtualizacao);

public sealed record GoogleAdsSincronizacaoResponse(
    Guid Id,
    Guid? PublicacaoId,
    TipoSincronizacaoGoogleAds Tipo,
    StatusSincronizacaoGoogleAds Status,
    int RegistrosConsultados,
    int RegistrosCriados,
    int RegistrosAtualizados,
    string? RequestId,
    string? ErroMensagemControlada,
    long DuracaoMs);

public sealed record GoogleAdsStatusRemotoResponse(
    Guid PublicacaoId,
    string StatusPublicacao,
    string? CampaignStatus,
    string? CampaignName,
    decimal? OrcamentoDiario,
    DateTime? UltimaSincronizacao,
    IReadOnlyList<string> AlteracoesExternas,
    IReadOnlyList<string> Pendencias);

public sealed record GoogleAdsMetricaDiariaResponse(
    DateOnly Data,
    long Impressoes,
    long Cliques,
    decimal Custo,
    decimal Ctr,
    decimal CpcMedio,
    decimal Conversoes,
    decimal ValorConversoes,
    decimal TaxaConversao);

public sealed record GoogleAdsDashboardResumoResponse(
    int CampanhasPublicadas,
    int CampanhasAtivas,
    int CampanhasPausadas,
    long Impressoes,
    long Cliques,
    decimal Ctr,
    decimal Custo,
    decimal CpcMedio,
    decimal Conversoes,
    decimal ValorConversoes,
    int Leads,
    decimal CustoPorLead,
    decimal TaxaConversao,
    decimal Roas,
    DateTime? UltimaSincronizacao,
    string QualidadeAtribuicao);

public sealed record GoogleAdsDashboardCampanhaResponse(
    Guid PublicacaoId,
    string Campanha,
    string Status,
    long Impressoes,
    long Cliques,
    decimal Ctr,
    decimal Custo,
    decimal Conversoes,
    int Leads,
    decimal CustoPorLead,
    DateTime? UltimaSincronizacao);

public sealed record GoogleAdsEvolucaoResponse(DateOnly Data, long Cliques, decimal Custo, decimal Conversoes, int Leads);
public sealed record GoogleAdsAtribuicaoResponse(TipoAtribuicaoLead Tipo, int Leads);

public sealed record GoogleAdsOptimizationResult(
    string Resumo,
    IReadOnlyList<string> Diagnostico,
    IReadOnlyList<string> PontosFortes,
    IReadOnlyList<string> Problemas,
    IReadOnlyList<string> HeadlinesSugeridas,
    IReadOnlyList<string> DescriptionsSugeridas,
    IReadOnlyList<string> KeywordsSugeridas,
    IReadOnlyList<string> NegativasSugeridas,
    GoogleAdsBudgetRecommendation? RecomendacaoOrcamento,
    string? RecomendacaoLance,
    IReadOnlyList<string> AcoesPrioritarias,
    decimal NivelConfianca);

public sealed record GoogleAdsBudgetRecommendation(decimal ValorAtual, decimal ValorSugerido, string Justificativa);

public sealed record GoogleAdsAnaliseResponse(
    Guid Id,
    Guid PublicacaoId,
    DateOnly PeriodoInicial,
    DateOnly PeriodoFinal,
    string? Modelo,
    string? Provider,
    string Resumo,
    GoogleAdsOptimizationResult Resultado,
    long DuracaoMs,
    DateTime DataCriacao,
    bool Aplicada);

public sealed record GoogleAdsCriarPreviewPorAnaliseRequest(
    bool AplicarHeadlines,
    bool AplicarDescriptions,
    bool AplicarKeywords,
    bool AplicarNegativas,
    bool AplicarOrcamento);

public sealed record GoogleAdsRemoteStatusSnapshot(
    string CampaignResourceName,
    string? CampaignName,
    string? CampaignStatus,
    decimal? DailyBudget,
    string? BiddingStrategy,
    string? AdGroupStatus,
    string? AdStatus,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> NegativeKeywords,
    string? FinalUrl,
    IReadOnlyList<string> MissingResources,
    IReadOnlyList<string> ExternalChanges,
    string? RequestId);

public sealed record GoogleAdsMetricsRow(
    string CampaignResourceName,
    string CampaignExternalId,
    DateOnly Data,
    long Impressoes,
    long Cliques,
    long CustoMicros,
    decimal Ctr,
    long CpcMedioMicros,
    decimal Conversoes,
    decimal ValorConversoes,
    decimal? ParcelaImpressoesPesquisa,
    decimal? TaxaTopoPagina,
    decimal? TaxaTopoAbsoluto);
