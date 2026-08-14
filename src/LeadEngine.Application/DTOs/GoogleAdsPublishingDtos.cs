using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record GoogleAdsRemoteValidationResponse(
    bool Valido,
    string? RequestId,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Erros,
    IReadOnlyList<string> Avisos,
    DateTime DataValidacao,
    bool Sucesso = true,
    string? Codigo = null,
    string? Mensagem = null,
    string? StackTrace = null);

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
    string? AcaoSugerida,
    string? Location = null,
    IReadOnlyList<string>? FieldPathElements = null,
    string? Trigger = null,
    string? StatusCode = null,
    string? Detail = null);

public sealed record GoogleAdsDiagnosticResponse(
    bool Sucesso,
    string Codigo,
    string Mensagem,
    string? RequestId,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Erros,
    string? StatusCode = null,
    string? Detail = null,
    string? StackTrace = null);

public sealed record GoogleAdsPublishedResourceDto(
    string TipoRecurso,
    string ResourceName,
    string? ExternalId,
    string? Nome,
    string Status);

public sealed record GoogleAdsPublishedResourceCheckDto(
    string TipoRecurso,
    string ResourceName,
    string? ExternalId,
    string? Nome,
    string Status,
    bool Encontrado,
    bool AlteradoExternamente,
    string? Observacao);

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
    string Orientacao,
    int RecursosEsperados = 0,
    int RecursosEncontrados = 0,
    int RecursosAusentes = 0,
    IReadOnlyList<string>? AlteracoesExternas = null,
    bool RequerIntervencao = false);

public sealed record GoogleAdsDryRunResponse(
    IReadOnlyList<GoogleAdsDryRunOperationDto> Operacoes,
    int QuantidadeOperacoes,
    bool Valido,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Erros,
    IReadOnlyList<string> Avisos);

public sealed record GoogleAdsDryRunOperationDto(
    int Indice,
    string Tipo,
    string Status,
    string? ResourceNameTemporario);

public sealed record GoogleAdsPublicationHistoryResponse(
    Guid Id,
    StatusPublicacaoGoogleAds? StatusAnterior,
    StatusPublicacaoGoogleAds StatusNovo,
    string Operacao,
    string? MensagemControlada,
    string? RequestId,
    DateTime Data);

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
    string PayloadJson,
    string? ResourceNameTemporario = null);

public sealed record GoogleAdsMutationResult(
    bool Success,
    string? RequestId,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Resources,
    IReadOnlyList<GoogleAdsPublicationErrorDto> Errors,
    bool EvidenceOfPartialCreation);
