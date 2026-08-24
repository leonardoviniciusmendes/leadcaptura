using System.Diagnostics;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsSynchronizationService(
    IGoogleAdsPublicationRepository publicationRepository,
    IGoogleAdsContaRepository contaRepository,
    IGoogleAdsSynchronizationRepository syncRepository,
    IGoogleAdsSynchronizationQueryClient queryClient,
    IGoogleAdsTokenService tokenService,
    IConfigurationResolver resolver) : IGoogleAdsSynchronizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsSincronizacaoResponse> SincronizarPublicacaoAsync(Guid publicacaoId, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.ObterPorIdAsync(publicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var sync = NewSync(publication, TipoSincronizacaoGoogleAds.Publicacao);
        await syncRepository.AdicionarAsync(sync, cancellationToken);
        await syncRepository.SalvarAsync(cancellationToken);
        var sw = Stopwatch.StartNew();
        try
        {
            var snapshot = await SnapshotAsync(publication, cancellationToken);
            sync.RequestId = snapshot.RequestId;
            sync.RegistrosConsultados = publication.Recursos.Count;
            sync.RegistrosAtualizados = 1;
            publication.DataAtualizacao = DateTime.UtcNow;
            publication.RecursosJson = JsonSerializer.Serialize(publication.Recursos.Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status)), JsonOptions);
            if (snapshot.MissingResources.Count > 0)
            {
                publication.Status = StatusPublicacaoGoogleAds.RequerIntervencao;
                sync.Status = StatusSincronizacaoGoogleAds.ConcluidaComAvisos;
                sync.ErroMensagemControlada = "Recursos ausentes no Google Ads. Reconciliacao manual necessaria.";
            }
            else
            {
                sync.Status = StatusSincronizacaoGoogleAds.Concluida;
            }
        }
        catch (Exception ex)
        {
            sync.Status = StatusSincronizacaoGoogleAds.Falhou;
            sync.ErroCodigo = ex is GoogleAdsDiagnosticException diagnosticException ? diagnosticException.Diagnostic.Codigo : "google_ads_sync_error";
            sync.ErroMensagemControlada = ex is GoogleAdsDiagnosticException diagnosticException2 ? diagnosticException2.Diagnostic.Mensagem : ex.Message;
            if (ex is GoogleAdsDiagnosticException)
            {
                throw;
            }
        }
        finally
        {
            sw.Stop();
            sync.DuracaoMs = sw.ElapsedMilliseconds;
            sync.DataConclusao = DateTime.UtcNow;
            await publicationRepository.SalvarAsync(cancellationToken);
            await syncRepository.SalvarAsync(cancellationToken);
        }
        return ToResponse(sync);
    }

    public async Task<GoogleAdsStatusRemotoResponse> ObterStatusRemotoAsync(Guid publicacaoId, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.ObterPorIdAsync(publicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var snapshot = await SnapshotAsync(publication, cancellationToken);
        return new GoogleAdsStatusRemotoResponse(publication.Id, publication.Status.ToString(), snapshot.CampaignStatus, snapshot.CampaignName, snapshot.DailyBudget, publication.DataAtualizacao, snapshot.ExternalChanges, snapshot.MissingResources);
    }

    public async Task<IReadOnlyList<GoogleAdsSincronizacaoResponse>> SincronizarTodasAsync(CancellationToken cancellationToken)
    {
        var publications = await publicationRepository.ListarAsync(new GoogleAdsPublicationQuery(null, null, null, null, null), cancellationToken);
        var result = new List<GoogleAdsSincronizacaoResponse>();
        foreach (var publication in publications.Where(x => x.Recursos.Count > 0))
        {
            result.Add(await SincronizarPublicacaoAsync(publication.Id, cancellationToken));
        }
        return result;
    }

    public async Task<GoogleAdsPublicationResponse> PausarAsync(Guid publicacaoId, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.ObterPorIdAsync(publicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        await EnsureTestMutationAllowedAsync(publication, cancellationToken);
        await SetStatusRemoteAsync(publication, "PAUSED", cancellationToken);
        await SincronizarPublicacaoAsync(publication.Id, cancellationToken);
        return ToPublicationResponse(publication);
    }

    public async Task<GoogleAdsPublicationResponse> AtivarAsync(Guid publicacaoId, GoogleAdsStatusActionRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConfirmarAtivacaoEmContaTeste) throw new ArgumentException("Confirme ativacao em conta de teste.");
        var publication = await publicationRepository.ObterPorIdAsync(publicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        if (publication.Status != StatusPublicacaoGoogleAds.Reconciliada)
        {
            throw new InvalidOperationException("Somente publicacao reconciliada pode ser ativada pelo painel.");
        }

        var allow = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "AllowTestAccountActivation", cancellationToken)).Value, out var value) && value;
        if (!allow) throw new UnauthorizedAccessException("Ativacao em conta de teste desabilitada por configuracao.");
        await EnsureTestMutationAllowedAsync(publication, cancellationToken);
        var snapshot = await SnapshotAsync(publication, cancellationToken);
        if (snapshot.MissingResources.Count > 0)
        {
            throw new InvalidOperationException("Recursos remotos ausentes. Execute reconciliacao antes de ativar.");
        }

        if (!string.Equals(snapshot.CampaignStatus, "PAUSED", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(snapshot.CampaignStatus, "ENABLED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Campaign remota precisa estar PAUSED antes da ativacao.");
        }

        var targetResources = ResourcesForActivation(publication);
        string? requestId = null;
        if (string.Equals(snapshot.CampaignStatus, "ENABLED", StringComparison.OrdinalIgnoreCase)
            && targetResources.All(x => string.Equals(x.Status, "ENABLED", StringComparison.OrdinalIgnoreCase)))
        {
            requestId = snapshot.RequestId;
        }
        else
        {
            var conta = await contaRepository.ObterPorIdAsync(publication.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
            var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
            var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
            requestId = await queryClient.SetResourceStatusesAsync(publication.CustomerId, accessToken, developerToken, targetResources, "ENABLED", cancellationToken);
            foreach (var resource in publication.Recursos.Where(x => targetResources.Any(y => y.ResourceName == x.ResourceName)))
            {
                resource.Status = "ENABLED";
            }
        }

        var activationHistory = await publicationRepository.ListarHistoricoAsync(publication.Id, cancellationToken);
        if (!activationHistory.Any(x => x.Operacao == "Ativacao"))
        {
            await publicationRepository.AdicionarHistoricoAsync(new GoogleAdsPublicacaoHistorico
            {
                Id = Guid.NewGuid(),
                GoogleAdsPublicacaoId = publication.Id,
                StatusAnterior = publication.Status,
                StatusNovo = publication.Status,
                Operacao = "Ativacao",
                MensagemControlada = "Recursos da publicacao ativados no Google Ads.",
                RequestId = requestId,
                Data = DateTime.UtcNow,
                MetadadosJson = JsonSerializer.Serialize(new { target = "ENABLED", resources = targetResources.Select(x => x.ResourceName).ToArray() }, JsonOptions)
            }, cancellationToken);
        }

        publication.DataAtualizacao = DateTime.UtcNow;
        publication.RecursosJson = JsonSerializer.Serialize(publication.Recursos.Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status)), JsonOptions);
        await publicationRepository.SalvarAsync(cancellationToken);
        await SincronizarPublicacaoAsync(publication.Id, cancellationToken);
        return ToPublicationResponse(publication);
    }

    private static IReadOnlyList<GoogleAdsPublishedResourceDto> ResourcesForActivation(GoogleAdsPublicacao publication)
    {
        var resources = publication.Recursos
            .Where(x => x.TipoRecurso is "AdGroup" or "Keyword" or "ResponsiveSearchAd" or "Campaign")
            .OrderBy(ActivationOrder)
            .Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status))
            .ToArray();
        if (!resources.Any(x => x.TipoRecurso == "Campaign"))
        {
            throw new InvalidOperationException("Campaign resource name ausente.");
        }

        return resources;
    }

    private static int ActivationOrder(GoogleAdsRecursoPublicado resource) => resource.TipoRecurso switch
    {
        "AdGroup" => 0,
        "Keyword" => 1,
        "ResponsiveSearchAd" => 2,
        "Campaign" => 3,
        _ => 10
    };

    private async Task<GoogleAdsRemoteStatusSnapshot> SnapshotAsync(GoogleAdsPublicacao publication, CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterPorIdAsync(publication.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
        return await queryClient.GetRemoteStatusAsync(publication.CustomerId, accessToken, developerToken, publication.Recursos.Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status)).ToArray(), cancellationToken);
    }

    private async Task SetStatusRemoteAsync(GoogleAdsPublicacao publication, string status, CancellationToken cancellationToken)
    {
        var campaign = publication.Recursos.FirstOrDefault(x => x.TipoRecurso == "Campaign")?.ResourceName ?? throw new InvalidOperationException("Campaign resource name ausente.");
        var conta = await contaRepository.ObterPorIdAsync(publication.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
        await queryClient.SetCampaignStatusAsync(publication.CustomerId, accessToken, developerToken, campaign, status, cancellationToken);
    }

    private async Task EnsureTestMutationAllowedAsync(GoogleAdsPublicacao publication, CancellationToken cancellationToken)
    {
        var enabled = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "EnableRealPublishing", cancellationToken)).Value, out var flag) && flag;
        var useTest = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "UseTestAccount", cancellationToken)).Value, out var test) && test;
        var testCustomer = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "TestCustomerId", cancellationToken)).Value;
        if (!enabled || !useTest) throw new UnauthorizedAccessException("Operacao remota bloqueada fora do modo de teste habilitado.");
        if (!GoogleAdsCustomerId.TryNormalize(testCustomer, out var normalizedTest) || GoogleAdsCustomerId.DigitsOnly(publication.CustomerId) != normalizedTest) throw new UnauthorizedAccessException("Operacao remota bloqueada para CustomerId diferente da conta de teste.");
    }

    private async Task<string> RequiredSecretAsync(string key, CancellationToken cancellationToken)
    {
        var value = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, key, cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{key} nao configurado.");
        return value;
    }

    private static GoogleAdsSincronizacao NewSync(GoogleAdsPublicacao p, TipoSincronizacaoGoogleAds type) => new() { Id = Guid.NewGuid(), GoogleAdsPublicacaoId = p.Id, GoogleAdsContaId = p.GoogleAdsContaId, Tipo = type, Status = StatusSincronizacaoGoogleAds.Executando, DataInicio = DateTime.UtcNow, DataCriacao = DateTime.UtcNow };
    private static GoogleAdsSincronizacaoResponse ToResponse(GoogleAdsSincronizacao x) => new(x.Id, x.GoogleAdsPublicacaoId, x.Tipo, x.Status, x.RegistrosConsultados, x.RegistrosCriados, x.RegistrosAtualizados, x.RequestId, x.ErroMensagemControlada, x.DuracaoMs);
    private static IReadOnlyList<GoogleAdsPublicationErrorDto> DeserializeErrors(string json) => JsonSerializer.Deserialize<IReadOnlyList<GoogleAdsPublicationErrorDto>>(json, JsonOptions) ?? [];
    private static GoogleAdsPublicationResponse ToPublicationResponse(GoogleAdsPublicacao p) => new(p.Id, p.GoogleAdsPlanoPublicacaoId, p.CampanhaId, p.GoogleAdsContaId, GoogleAdsCustomerId.Mask(p.CustomerId), p.PreviewVersao, p.PreviewHash, p.Status, p.RequestIdValidacao, p.RequestIdPublicacao, p.ErroCodigo, p.ErroMensagemControlada, DeserializeErrors(p.ErrosJson), p.Recursos.Select(x => new GoogleAdsPublishedResourceDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status)).ToArray(), p.DataCriacao, p.DataAtualizacao, p.Teste);
}
