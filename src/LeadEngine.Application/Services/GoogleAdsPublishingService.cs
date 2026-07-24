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
    IGoogleAdsTokenService tokenService,
    IConfigurationResolver resolver) : IGoogleAdsPublishingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsRemoteValidationResponse> ValidarRemotamenteAsync(Guid previewId, CancellationToken cancellationToken)
    {
        var context = await LoadAndValidateAsync(previewId, requireRemoteValidation: false, cancellationToken);
        var publication = await GetOrCreatePublicationAsync(context, StatusPublicacaoGoogleAds.ValidandoRemotamente, cancellationToken);
        publication.Status = StatusPublicacaoGoogleAds.ValidandoRemotamente;
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        var accessToken = await tokenService.ObterAccessTokenValidoAsync(context.Conta, cancellationToken);
        var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
        var plan = await operationBuilder.BuildAsync(context.Preview, context.Conta.CustomerId, cancellationToken);
        publication.GeoTargetResourceName = plan.GeoTargetResourceName;
        publication.LanguageResourceName = plan.LanguageResourceName;

        var result = await mutationClient.MutateAsync(context.Conta.CustomerId, accessToken, developerToken, plan, validateOnly: true, cancellationToken);
        publication.RequestIdValidacao = result.RequestId;
        publication.DataValidacaoRemota = DateTime.UtcNow;
        publication.ErrosJson = Serialize(result.Errors);
        publication.Status = result.Success ? StatusPublicacaoGoogleAds.Validada : StatusPublicacaoGoogleAds.Falhou;
        publication.ErroCodigo = result.Errors.FirstOrDefault()?.Codigo;
        publication.ErroMensagemControlada = result.Errors.FirstOrDefault()?.Mensagem;
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        return new GoogleAdsRemoteValidationResponse(result.Success, result.RequestId, result.Errors, plan.Avisos, publication.DataValidacaoRemota.Value);
    }

    public async Task<GoogleAdsPreparePublicationResponse> PrepararAsync(Guid previewId, CancellationToken cancellationToken)
    {
        var context = await LoadAndValidateAsync(previewId, requireRemoteValidation: false, cancellationToken);
        var publication = await GetOrCreatePublicationAsync(context, StatusPublicacaoGoogleAds.Preparada, cancellationToken);
        var token = NewToken();
        publication.ConfirmationTokenHash = Hash(token);
        publication.ConfirmationExpiresAt = DateTime.UtcNow.AddMinutes(10);
        publication.DataPreparacao = DateTime.UtcNow;
        publication.Status = publication.Status == StatusPublicacaoGoogleAds.Validada ? StatusPublicacaoGoogleAds.Validada : StatusPublicacaoGoogleAds.Preparada;
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

        publication.Status = StatusPublicacaoGoogleAds.Publicando;
        publication.DataInicioPublicacao = DateTime.UtcNow;
        publication.Tentativas++;
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);

        try
        {
            var plan = await operationBuilder.BuildAsync(context.Preview, context.Conta.CustomerId, cancellationToken);
            var accessToken = await tokenService.ObterAccessTokenValidoAsync(context.Conta, cancellationToken);
            var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
            var result = await mutationClient.MutateAsync(context.Conta.CustomerId, accessToken, developerToken, plan, validateOnly: false, cancellationToken);
            publication.RequestIdPublicacao = result.RequestId;
            publication.ErrosJson = Serialize(result.Errors);
            publication.RecursosJson = Serialize(result.Resources);
            publication.DataConclusao = DateTime.UtcNow;
            publication.DataAtualizacao = DateTime.UtcNow;
            publication.Status = result.Success
                ? StatusPublicacaoGoogleAds.Publicada
                : result.EvidenceOfPartialCreation ? StatusPublicacaoGoogleAds.ParcialmentePublicada : StatusPublicacaoGoogleAds.Falhou;
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
            await publicationRepository.SalvarAsync(cancellationToken);
            return ToResponse(publication);
        }
        catch (Exception ex)
        {
            publication.Status = publication.Recursos.Any() ? StatusPublicacaoGoogleAds.RequerIntervencao : StatusPublicacaoGoogleAds.Falhou;
            publication.ErroCodigo = "google_ads_error";
            publication.ErroMensagemControlada = "Falha ao publicar no Google Ads.";
            publication.ErrosJson = Serialize(new[] { new GoogleAdsPublicationErrorDto("google_ads_error", "Falha ao publicar no Google Ads.", null, null, null, null, null, true, "Valide a conta e tente novamente.") });
            publication.DataAtualizacao = DateTime.UtcNow;
            await publicationRepository.SalvarAsync(cancellationToken);
            throw new InvalidOperationException(publication.ErroMensagemControlada, ex);
        }
    }

    public async Task<GoogleAdsReconciliationResponse> ReconciliarAsync(Guid id, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var conta = await contaRepository.ObterPorIdAsync(publication.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
        var resources = DeserializeResources(publication.RecursosJson);
        var checkedResources = await mutationClient.CheckResourcesAsync(conta.CustomerId, accessToken, developerToken, resources, cancellationToken);
        publication.RecursosJson = Serialize(checkedResources);
        publication.Status = checkedResources.Count == resources.Count && checkedResources.Count > 0 ? StatusPublicacaoGoogleAds.Publicada : StatusPublicacaoGoogleAds.RequerIntervencao;
        publication.DataAtualizacao = DateTime.UtcNow;
        await publicationRepository.SalvarAsync(cancellationToken);
        return new GoogleAdsReconciliationResponse(publication.Id, publication.Status, checkedResources, "Nao cria recursos ausentes automaticamente. Revise no Google Ads antes de nova tentativa.");
    }

    public async Task<GoogleAdsPublicationResponse> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await publicationRepository.ObterPorIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada."));
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
        if (useTest && string.IsNullOrWhiteSpace(testCustomer)) errors.Add("TestCustomerId obrigatorio quando UseTestAccount=true.");
        if (useTest && Digits(testCustomer) != Digits(conta.CustomerId)) errors.Add("Modo teste bloqueia publicacao fora do TestCustomerId.");
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
        await publicationRepository.AdicionarAsync(publication, cancellationToken);
        return publication;
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
    private static string MaskCustomerId(string value) => value.Length <= 4 ? "****" : $"{value[..2]}****{value[^2..]}";
    private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
    private static GoogleAdsPreviewPayload Payload(GoogleAdsPlanoPublicacao preview) => JsonSerializer.Deserialize<GoogleAdsPreviewPayload>(preview.PayloadPreviewJson, JsonOptions)!;
    private static IReadOnlyList<GoogleAdsPublicationErrorDto> DeserializeErrors(string json) => JsonSerializer.Deserialize<IReadOnlyList<GoogleAdsPublicationErrorDto>>(json, JsonOptions) ?? [];
    private static IReadOnlyList<GoogleAdsPublishedResourceDto> DeserializeResources(string json) => JsonSerializer.Deserialize<IReadOnlyList<GoogleAdsPublishedResourceDto>>(json, JsonOptions) ?? [];
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record PublicationContext(GoogleAdsPlanoPublicacao Preview, Campanha Campanha, GoogleAdsConta Conta, bool UseTestAccount);
}
