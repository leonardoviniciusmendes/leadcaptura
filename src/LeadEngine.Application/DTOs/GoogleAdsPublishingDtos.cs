using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record GoogleAdsRemoteValidationResponse(
    bool Valido,
    string? RequestId,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Erros,
    IReadOnlyList<string> Avisos,
    DateTime DataValidacao);

public sealed record GoogleAdsPreparePublicationResponse(
    Guid PublicacaoId,
    string ConfirmationToken,
    string Nome,
    string Conta,
    string CustomerIdMascarado,
    decimal OrcamentoDiario,
    int QuantidadeGrupos,
    int QuantidadeKeywords,
    int QuantidadeNegativas,
    int QuantidadeAnuncios,
    string Url,
    string StatusPlanejado,
    string Hash,
    int Versao,
    bool ValidacaoLocal,
    bool ValidacaoRemota,
    bool Teste);

public sealed record GoogleAdsPublishRequest(string ConfirmationToken, bool ConfirmarCriacaoPausada);

public sealed record GoogleAdsPublicationResponse(
    Guid Id,
    Guid PreviewId,
    Guid CampanhaId,
    Guid ContaId,
    string CustomerIdMascarado,
    int PreviewVersao,
    string PreviewHash,
    StatusPublicacaoGoogleAds Status,
    string? RequestIdValidacao,
    string? RequestIdPublicacao,
    string? ErroCodigo,
    string? ErroMensagemControlada,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Erros,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Recursos,
    DateTime DataCriacao,
    DateTime? DataAtualizacao,
    bool Teste);

public sealed record GoogleAdsPublicationErrorDto(
    string Codigo,
    string Mensagem,
    string? Operacao,
    int? IndiceOperacao,
    string? Campo,
    string? ValorRejeitado,
    string? RequestId,
    bool Recuperavel,
    string? AcaoSugerida);

public sealed record GoogleAdsPublishedResourceDto(
    string TipoRecurso,
    string ResourceName,
    string? ExternalId,
    string? Nome,
    string Status);

public sealed record GoogleAdsPublicationQuery(
    StatusPublicacaoGoogleAds? Status,
    Guid? CampanhaId,
    Guid? ContaId,
    DateTime? DataInicial,
    DateTime? DataFinal);

public sealed record GoogleAdsReconciliationResponse(
    Guid PublicacaoId,
    StatusPublicacaoGoogleAds Status,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Recursos,
    string Orientacao);

public sealed record GoogleAdsOperationPlan(
    string PreviewHash,
    int PreviewVersao,
    string CustomerId,
    string GeoTargetResourceName,
    string LanguageResourceName,
    IReadOnlyList<GoogleAdsOperationItem> Operations,
    IReadOnlyList<string> Avisos);

public sealed record GoogleAdsOperationItem(
    string TipoRecurso,
    string Nome,
    string Operation,
    string PayloadJson);

public sealed record GoogleAdsMutationResult(
    bool Success,
    string? RequestId,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Resources,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Errors,
    bool EvidenceOfPartialCreation);
