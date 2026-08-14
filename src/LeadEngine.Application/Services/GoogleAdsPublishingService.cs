using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsPublishingService(
    IGoogleAdsPlanoPublicacaoRepository previewRepository,
    ICampanhaRepository campanhaRepository,
    IGoogleAdsContaRepository contaRepository,
    IGoogleAdsPublicationRepository publicationRepository,
    IGoogleAdsOperationBuilder operationBuilder,
    IGoogleAdsMutationClient mutationClient,
    IGoogleAdsResourceQueryClient resourceQueryClient,
    IGoogleAdsTokenService tokenService,
    IConfigurationResolver resolver) : IGoogleAdsPublishingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsRemoteValidationResponse> ValidarRemotamenteAsync(Guid previewId, CancellationToken cancellationToken)
    {
        var context = await LoadAndValidateAsync(previewId, requireRemoteValidation: false, cancellationToken);
        var publication = await GetOrCreatePublicationAsync(context, StatusPublicacaoGoogleAds.ValidandoRemotamente, cancellationToken);
        await SetStatusAsync(publication, StatusPublicacaoGoogleAds.ValidandoRemotamente, "ValidacaoRemota", null, null, cancellationToken);
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        var accessToken = await tokenService.ObterAccessTokenValidoAsync(context.Conta, cancellationToken);
        var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
        var plan = await operationBuilder.BuildAsync(context.Preview, context.Conta.CustomerId, cancellationToken);
        publication.GeoTargetResourceName = plan.GeoTargetResourceName;
        publication.LanguageResourceName = plan.LanguageResourceName;
        await AuditarOperacoesAsync(publication, plan, "ValidateOnly", cancellationToken);

        var result = await mutationClient.MutateAsync(context.Conta.CustomerId, accessToken, developerToken, plan, validateOnly: true, cancellationToken);
        publication.RequestIdValidacao = result.RequestId;
        publication.DataValidacaoRemota = DateTime.UtcNow;
        publication.ErrosJson = Serialize(result.Errors);
        await SetStatusAsync(publication, result.Success ? StatusPublicacaoGoogleAds.Validada : StatusPublicacaoGoogleAds.Falhou, "ValidateOnly", result.Success ? "Validacao remota aprovada." : "Validacao remota falhou.", result.RequestId, cancellationToken);
        publication.ErroCodigo = result.Errors.FirstOrDefault()?.Codigo;
        publication.ErroMensagemControlada = result.Errors.FirstOrDefault()?.Mensagem;
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        return new GoogleAdsRemoteValidationResponse(
            result.Success,
            result.RequestId,
            result.Errors,
            plan.Avisos,
            publication.DataValidacaoRemota.Value,
            result.Success,
            result.Errors.FirstOrDefault()?.Codigo,
            result.Success ? "Validacao remota aprovada." : result.Errors.FirstOrDefault()?.Mensagem ?? "Validacao remota falhou.");
    }

    public async Task<GoogleAdsDryRunResponse> DryRunAsync(Guid previewId, CancellationToken cancellationToken)
    {
        var context = await LoadAndValidateAsync(previewId, requireRemoteValidation: false, cancellationToken);
        var plan = await operationBuilder.BuildAsync(context.Preview, context.Conta.CustomerId, cancellationToken);
        var operations = plan.Operations.Select((x, i) => new GoogleAdsDryRunOperationDto(i, x.TipoRecurso, "PAUSED", x.ResourceNameTemporario)).ToArray();
        return new GoogleAdsDryRunResponse(operations, operations.Length, true, [], plan.Avisos);
    }

    public async Task<GoogleAdsPreparePublicationResponse> PrepararAsync(Guid previewId, CancellationToken cancellationToken)
    {
        var context = await LoadAndValidateAsync(previewId, requireRemoteValidation: false, cancellationToken);
        var publication = await GetOrCreatePublicationAsync(context, StatusPublicacaoGoogleAds.Preparada, cancellationToken);
        var token = NewToken();
        publication.ConfirmationTokenHash = Hash(token);
        publication.ConfirmationExpiresAt = DateTime.UtcNow.AddMinutes(10);
        publication.DataPreparacao = DateTime.UtcNow;
        await SetStatusAsync(publication, publication.Status == StatusPublicacaoGoogleAds.Validada ? StatusPublicacaoGoogleAds.Validada : StatusPublicacaoGoogleAds.Preparada, "Preparacao", "Publicacao preparada com token de confirmacao.", null, cancellationToken);
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        var payload = Payload(context.Preview);
        var group = payload.AdGroups.First();
        return new GoogleAdsPreparePublicationResponse(
            publication.Id,
            token,
            context.Preview.NomeCampanha,
            context.Conta.Nome,
            MaskCustomerId(context.Conta.CustomerId),
            context.Preview.OrcamentoDiario,
            payload.AdGroups.Count,
            group.Keywords.Count,
            group.NegativeKeywords.Count,
            payload.AdGroups.Count,
            context.Preview.UrlFinal,
            "PAUSED",
            context.Preview.ConteudoHash,
            context.Preview.Versao,
            context.Preview.Status == StatusPlanoPublicacaoGoogleAds.Valido,
            publication.Status == StatusPublicacaoGoogleAds.Validada,
            publication.Teste);
    }

    public async Task<GoogleAdsPublicationResponse> PublicarAsync(Guid previewId, GoogleAdsPublishRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConfirmarCriacaoPausada)
        {
            throw new ArgumentException("Confirme a criacao da campanha em estado pausado.");
        }

        await EnsureRealPublishingAllowedAsync(cancellationToken);

        var previewForIdempotency = await previewRepository.ObterPorIdAsync(previewId, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        var existingPublished = await publicationRepository.ObterPorPreviewVersaoHashAsync(previewForIdempotency.Id, previewForIdempotency.Versao, previewForIdempotency.ConteudoHash, cancellationToken);
        if (existingPublished?.Status == StatusPublicacaoGoogleAds.Publicada)
        {
            return ToResponse(existingPublished);
        }

        var context = await LoadAndValidateAsync(previewId, requireRemoteValidation: true, cancellationToken);
        var publication = await publicationRepository.ObterPorPreviewVersaoHashAsync(context.Preview.Id, context.Preview.Versao, context.Preview.ConteudoHash, cancellationToken)
            ?? throw new ArgumentException("Prepare a publicacao antes de publicar.");
        if (publication.Status == StatusPublicacaoGoogleAds.Publicada)
        {
            return ToResponse(publication);
        }
        if (publication.Status == StatusPublicacaoGoogleAds.Publicando)
        {
            throw new InvalidOperationException("Publicacao ja esta em andamento.");
        }
        if (publication.Status == StatusPublicacaoGoogleAds.ParcialmentePublicada || publication.Status == StatusPublicacaoGoogleAds.RequerIntervencao)
        {
            throw new InvalidOperationException("Publicacao exige reconciliacao antes de nova tentativa.");
        }
        if (publication.ConfirmationExpiresAt <= DateTime.UtcNow || publication.ConfirmationTokenHash != Hash(request.ConfirmationToken))
        {
            throw new ArgumentException("Token de confirmacao invalido ou expirado.");
        }

        await SetStatusAsync(publication, StatusPublicacaoGoogleAds.Publicando, "Publicacao", "Inicio da publicacao controlada em PAUSED.", null, cancellationToken);
        publication.DataInicioPublicacao = DateTime.UtcNow;
        publication.Tentativas++;
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        try
        {
            var plan = await operationBuilder.BuildAsync(context.Preview, context.Conta.CustomerId, cancellationToken);
            await AuditarOperacoesAsync(publication, plan, "Publicacao", cancellationToken);
            var accessToken = await tokenService.ObterAccessTokenValidoAsync(context.Conta, cancellationToken);
            var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
            var result = await mutationClient.MutateAsync(context.Conta.CustomerId, accessToken, developerToken, plan, validateOnly: false, cancellationToken);
            publication.RequestIdPublicacao = result.RequestId;
            publication.ErrosJson = Serialize(result.Errors);
            publication.RecursosJson = Serialize(result.Resources);
            publication.DataConclusao = DateTime.UtcNow;
            publication.DataAtualizacao = DateTime.UtcNow;
            var newStatus = result.Success
                ? StatusPublicacaoGoogleAds.Publicada
                : result.EvidenceOfPartialCreation ? StatusPublicacaoGoogleAds.ParcialmentePublicada : StatusPublicacaoGoogleAds.Falhou;
            await SetStatusAsync(publication, newStatus, "Publicacao", result.Success ? "Recursos criados em PAUSED." : "Falha ao criar recursos.", result.RequestId, cancellationToken);
            publication.ErroCodigo = result.Errors.FirstOrDefault()?.Codigo;
            publication.ErroMensagemControlada = result.Errors.FirstOrDefault()?.Mensagem;
            foreach (var resource in result.Resources)
            {
                await publicationRepository.AdicionarRecursoAsync(new GoogleAdsRecursoPublicado
                {
                    Id = Guid.NewGuid(),
                    GoogleAdsPublicacaoId = publication.Id,
                    TipoRecurso = resource.TipoRecurso,
                    ResourceName = resource.ResourceName,
                    ExternalId = resource.ExternalId,
                    Nome = resource.Nome,
                    Status = resource.Status,
                    DataCriacao = DateTime.UtcNow
                }, cancellationToken);
            }
            AtualizarAuditoriaComResultados(publication, result);
            await publicationRepository.SalvarAsync(cancellationToken);
            return ToResponse(publication);
        }
        catch (Exception ex)
        {
            var diagnostic = ex is LeadEngine.Application.Common.GoogleAdsDiagnosticException googleAdsDiagnostic
                ? googleAdsDiagnostic.Diagnostic
                : new GoogleAdsDiagnosticResponse(false, "google_ads_error", ex.Message, null, [new GoogleAdsPublicationErrorDto("google_ads_error", ex.Message, null, null, null, null, null, true, "Valide a conta e tente novamente.")]);
            await SetStatusAsync(publication, publication.Recursos.Any() ? StatusPublicacaoGoogleAds.RequerIntervencao : StatusPublicacaoGoogleAds.Falhou, "ErroPublicacao", "Falha ao publicar no Google Ads.", null, cancellationToken);
            publication.ErroCodigo = diagnostic.Codigo;
            publication.ErroMensagemControlada = diagnostic.Mensagem;
            publication.ErrosJson = Serialize(diagnostic.Erros);
            publication.DataAtualizacao = DateTime.UtcNow;
            await publicationRepository.SalvarAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GoogleAdsReconciliationResponse> ReconciliarAsync(Guid id, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var conta = await contaRepository.ObterPorIdAsync(publication.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
        var resources = DeserializeResources(publication.RecursosJson);
        var checkedResources = await resourceQueryClient.CheckResourcesAsync(conta.CustomerId, accessToken, developerToken, resources, cancellationToken);
        var found = checkedResources.Where(x => x.Encontrado).Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status)).ToArray();
        publication.RecursosJson = Serialize(found);
        var newStatus = found.Length == resources.Count && found.Length > 0 ? StatusPublicacaoGoogleAds.Reconciliada : StatusPublicacaoGoogleAds.RequerIntervencao;
        await SetStatusAsync(publication, newStatus, "Reconciliacao", "Reconciliação por resource name concluida.", null, cancellationToken);
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);
        var changes = checkedResources.Where(x => x.AlteradoExternamente || !x.Encontrado).Select(x => $"{x.TipoRecurso}: {x.Observacao ?? "alteracao detectada"}").ToArray();
        return new GoogleAdsReconciliationResponse(publication.Id, publication.Status, found, "Nao cria recursos ausentes automaticamente. Revise no Google Ads antes de nova tentativa.", resources.Count, found.Length, resources.Count - found.Length, changes, publication.Status == StatusPublicacaoGoogleAds.RequerIntervencao);
    }

    public async Task<GoogleAdsPublicationResponse> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await publicationRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada."));
    }

    public async Task<IReadOnlyList<GoogleAdsPublicationHistoryResponse>> HistoricoAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await publicationRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var history = await publicationRepository.ListarHistoricoAsync(exists.Id, cancellationToken);
        return history.Select(x => new GoogleAdsPublicationHistoryResponse(x.Id, x.StatusAnterior, x.StatusNovo, x.Operacao, x.MensagemControlada, x.RequestId, x.Data)).ToArray();
    }

    public async Task<IReadOnlyList<GoogleAdsPublicationResponse>> ListarPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        return (await publicationRepository.ListarPorCampanhaAsync(campanhaId, cancellationToken)).Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<GoogleAdsPublicationResponse>> ListarAsync(GoogleAdsPublicationQuery query, CancellationToken cancellationToken)
    {
        return (await publicationRepository.ListarAsync(query, cancellationToken)).Select(ToResponse).ToArray();
    }

    private async Task<PublicationContext> LoadAndValidateAsync(Guid previewId, bool requireRemoteValidation, CancellationToken cancellationToken)
    {
        var preview = await previewRepository.ObterPorIdAsync(previewId, cancellationToken) ?? throw new KeyNotFoundException("Preview Google Ads nao encontrado.");
        var campanha = await campanhaRepository.ObterPorIdAsync(preview.CampanhaId, cancellationToken) ?? throw new KeyNotFoundException("Campanha nao encontrada.");
        var conta = await contaRepository.ObterPorIdAsync(preview.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
        var currentHash = preview.ConteudoHash;
        var errors = new List<string>();
        if (preview.Status != StatusPlanoPublicacaoGoogleAds.Valido) errors.Add("Preview precisa estar Valido.");
        if (campanha.Status != StatusCampanha.Revisada && campanha.Status != StatusCampanha.Publicada) errors.Add("Campanha original precisa continuar aprovada.");
        if (!campanha.Publicada || !campanha.Ativo) errors.Add("Landing precisa continuar publicada.");
        if (!Uri.TryCreate(preview.UrlFinal, UriKind.Absolute, out _)) errors.Add("URL final invalida.");
        if (!conta.Ativa || string.IsNullOrWhiteSpace(conta.CustomerId)) errors.Add("Conta Google Ads invalida.");
        if (!(await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "DeveloperToken", cancellationToken)).Configured) errors.Add("DeveloperToken nao configurado.");
        var useTest = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "UseTestAccount", cancellationToken)).Value, out var test) && test;
        var testCustomer = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "TestCustomerId", cancellationToken)).Value;
        if (useTest && !GoogleAdsCustomerId.TryNormalize(testCustomer, out var normalizedTestCustomer)) errors.Add("TestCustomerId obrigatorio quando UseTestAccount=true.");
        if (!GoogleAdsCustomerId.TryNormalize(conta.CustomerId, out var normalizedConta)) errors.Add("CustomerId da conta Google Ads invalido.");
        if (useTest && GoogleAdsCustomerId.TryNormalize(testCustomer, out normalizedTestCustomer) && normalizedConta != normalizedTestCustomer) errors.Add("Modo teste bloqueia publicacao fora do TestCustomerId.");
        var publication = await publicationRepository.ObterPorPreviewVersaoHashAsync(preview.Id, preview.Versao, currentHash, cancellationToken);
        if (requireRemoteValidation && publication?.Status != StatusPublicacaoGoogleAds.Validada) errors.Add("Validacao remota valida e atualizada obrigatoria.");
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        return new PublicationContext(preview, campanha, conta, useTest);
    }

    private async Task<GoogleAdsPublicacao> GetOrCreatePublicationAsync(PublicationContext context, StatusPublicacaoGoogleAds status, CancellationToken cancellationToken)
    {
        var existing = await publicationRepository.ObterPorPreviewVersaoHashAsync(context.Preview.Id, context.Preview.Versao, context.Preview.ConteudoHash, cancellationToken);
        if (existing is not null) return existing;
        var publication = new GoogleAdsPublicacao
        {
            Id = Guid.NewGuid(),
            GoogleAdsPlanoPublicacaoId = context.Preview.Id,
            CampanhaId = context.Preview.CampanhaId,
            GoogleAdsContaId = context.Conta.Id,
            CustomerId = context.Conta.CustomerId,
            PreviewVersao = context.Preview.Versao,
            PreviewHash = context.Preview.ConteudoHash,
            Status = status,
            IdempotencyKey = $"{context.Preview.Id:N}:{context.Preview.Versao}:{context.Preview.ConteudoHash}",
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow,
            Teste = context.UseTestAccount
        };
        publication.IsTestAccount = context.UseTestAccount;
        await publicationRepository.AdicionarAsync(publication, cancellationToken);
        await SetStatusAsync(publication, status, "CriacaoRegistro", "Registro de publicacao criado.", null, cancellationToken);
        return publication;
    }

    private async Task EnsureRealPublishingAllowedAsync(CancellationToken cancellationToken)
    {
        var enabled = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "EnableRealPublishing", cancellationToken)).Value, out var flag) && flag;
        if (!enabled)
        {
            throw new UnauthorizedAccessException("Publicacao real Google Ads desabilitada por feature flag.");
        }

        var useTest = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "UseTestAccount", cancellationToken)).Value, out var test) && test;
        if (!useTest)
        {
            throw new UnauthorizedAccessException("Publicacao em conta de producao bloqueada nesta etapa.");
        }
    }

    private async Task SetStatusAsync(GoogleAdsPublicacao publication, StatusPublicacaoGoogleAds status, string operation, string? message, string? requestId, CancellationToken cancellationToken)
    {
        var previous = publication.Status;
        publication.Status = status;
        publication.DataAtualizacao = DateTime.UtcNow;
        if (previous == status && publication.Historico.Any(x => x.Operacao == operation && x.StatusNovo == status))
        {
            return;
        }

        await publicationRepository.AdicionarHistoricoAsync(new GoogleAdsPublicacaoHistorico
        {
            Id = Guid.NewGuid(),
            GoogleAdsPublicacaoId = publication.Id,
            StatusAnterior = previous == default ? null : previous,
            StatusNovo = status,
            Operacao = operation,
            MensagemControlada = message,
            RequestId = requestId,
            Data = DateTime.UtcNow,
            MetadadosJson = "{}"
        }, cancellationToken);
    }

    private async Task AuditarOperacoesAsync(GoogleAdsPublicacao publication, GoogleAdsOperationPlan plan, string status, CancellationToken cancellationToken)
    {
        if (publication.Operacoes.Any(x => x.Status == status))
        {
            return;
        }

        var index = 0;
        foreach (var operation in plan.Operations)
        {
            await publicationRepository.AdicionarOperacaoAsync(new GoogleAdsOperacaoPublicacao
            {
                Id = Guid.NewGuid(),
                GoogleAdsPublicacaoId = publication.Id,
                Indice = index++,
                TipoOperacao = operation.Operation,
                EntidadeOrigem = operation.TipoRecurso,
                ResourceNameTemporario = operation.ResourceNameTemporario,
                Status = status,
                DataCriacao = DateTime.UtcNow
            }, cancellationToken);
        }
    }

    private static void AtualizarAuditoriaComResultados(GoogleAdsPublicacao publication, GoogleAdsMutationResult result)
    {
        var resources = result.Resources.ToArray();
        foreach (var op in publication.Operacoes.Where(x => x.Status == "Publicacao"))
        {
            var resource = op.Indice < resources.Length ? resources[op.Indice] : null;
            op.ResourceNameDefinitivo = resource?.ResourceName;
            op.Status = result.Success && resource is not null ? "Concluida" : "Inconsistente";
            op.CodigoErro = result.Errors.FirstOrDefault(x => x.IndiceOperacao == op.Indice)?.Codigo;
            op.MensagemControlada = result.Errors.FirstOrDefault(x => x.IndiceOperacao == op.Indice)?.Mensagem;
            op.DataConclusao = DateTime.UtcNow;
        }
    }

    private async Task<string> RequiredSecretAsync(string key, CancellationToken cancellationToken)
    {
        var value = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, key, cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{key} nao configurado.");
        return value;
    }

    private static GoogleAdsPublicationResponse ToResponse(GoogleAdsPublicacao publicacao) => new(
        publicacao.Id,
        publicacao.GoogleAdsPlanoPublicacaoId,
        publicacao.CampanhaId,
        publicacao.GoogleAdsContaId,
        MaskCustomerId(publicacao.CustomerId),
        publicacao.PreviewVersao,
        publicacao.PreviewHash,
        publicacao.Status,
        publicacao.RequestIdValidacao,
        publicacao.RequestIdPublicacao,
        publicacao.ErroCodigo,
        publicacao.ErroMensagemControlada,
        DeserializeErrors(publicacao.ErrosJson),
        publicacao.Recursos.Count > 0 ? publicacao.Recursos.Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status)).ToArray() : DeserializeResources(publicacao.RecursosJson),
        publicacao.DataCriacao,
        publicacao.DataAtualizacao,
        publicacao.Teste);

    private static string NewToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string MaskCustomerId(string value) => GoogleAdsCustomerId.Mask(value);
    private static GoogleAdsPreviewPayload Payload(GoogleAdsPlanoPublicacao preview) => JsonSerializer.Deserialize<GoogleAdsPreviewPayload>(preview.PayloadPreviewJson, JsonOptions)!;
    private static IReadOnlyList<GoogleAdsPublicationErrorDto> DeserializeErrors(string json) => JsonSerializer.Deserialize<IReadOnlyList<GoogleAdsPublicationErrorDto>>(json, JsonOptions) ?? [];
    private static IReadOnlyList<GoogleAdsPublishedResourceDto> DeserializeResources(string json) => JsonSerializer.Deserialize<IReadOnlyList<GoogleAdsPublishedResourceDto>>(json, JsonOptions) ?? [];
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record PublicationContext(GoogleAdsPlanoPublicacao Preview, Campanha Campanha, GoogleAdsConta Conta, bool UseTestAccount);
}
