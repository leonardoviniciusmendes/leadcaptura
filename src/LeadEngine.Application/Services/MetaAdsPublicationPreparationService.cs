using System.Security.Cryptography;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class MetaAdsPublicationPreparationService(
    ICampanhaRepository campanhaRepository,
    IMetaAdsContaRepository contaRepository,
    IMetaAdsAtivoSelecionadoRepository selecaoRepository,
    IMetaAdsImagemRepository imagemRepository,
    IMetaAdsPreparacaoPublicacaoRepository preparacaoRepository,
    IMetaAdsGraphClient graphClient,
    IConfigurationResolver resolver,
    ISecretProtector protector) : IMetaAdsPublicationPreparationService
{
    private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    public async Task<MetaAdsLocationSearchResponse> BuscarLocalizacoesAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = query.Trim();
        if (normalized.Length < 3)
        {
            return new MetaAdsLocationSearchResponse(false, [], "Digite pelo menos 3 caracteres.");
        }

        try
        {
            var (_, token, config) = await ContextAsync(cancellationToken);
            var country = await DefaultCountryAsync(cancellationToken);
            var items = await graphClient.SearchTargetingLocationsAsync(config, token, normalized, country, 10, cancellationToken);
            return new MetaAdsLocationSearchResponse(true, items, items.Count == 0 ? "Nenhuma localizacao Meta encontrada." : null);
        }
        catch (MetaAdsGraphApiException ex) when (ex.PermissionRequired)
        {
            return new MetaAdsLocationSearchResponse(false, [], "Permissao Meta insuficiente para buscar localizacoes de targeting.", true);
        }
        catch (MetaAdsGraphApiException ex)
        {
            return new MetaAdsLocationSearchResponse(false, [], ex.Message);
        }
    }

    public async Task<MetaAdsLocationResponse> SalvarTargetingAsync(MetaAdsTargetingSelectionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LocationKey))
        {
            throw new ArgumentException("Localizacao Meta obrigatoria.");
        }

        var campanha = await campanhaRepository.ObterPorIdAsync(request.CampanhaId, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");
        var (conta, token, config) = await ContextAsync(cancellationToken);
        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken)
            ?? throw new InvalidOperationException("Selecione os ativos Meta antes de configurar targeting.");
        if (string.IsNullOrWhiteSpace(selecao.AdAccountId))
        {
            throw new InvalidOperationException("Selecione uma Ad Account antes de configurar targeting.");
        }

        var country = await DefaultCountryAsync(cancellationToken);
        var query = string.IsNullOrWhiteSpace(campanha.Cidade) ? country : campanha.Cidade;
        var locations = await graphClient.SearchTargetingLocationsAsync(config, token, query, country, 25, cancellationToken);
        var location = locations.FirstOrDefault(x => Same(x.Key, request.LocationKey))
            ?? throw new InvalidOperationException("Localizacao Meta informada nao foi encontrada no Targeting Search.");
        var ageMin = request.IdadeMinima ?? 25;
        var ageMax = request.IdadeMaxima ?? 65;
        if (ageMin < 18 || ageMax < 18 || ageMin > 65 || ageMax > 65 || ageMin > ageMax)
        {
            throw new ArgumentException("Idade do publico Meta deve estar entre 18 e 65, com minimo menor ou igual ao maximo.");
        }

        var now = DateTime.UtcNow;
        var preparacao = await preparacaoRepository.ObterPorCampanhaAsync(campanha.Id, cancellationToken);
        if (preparacao is null)
        {
            preparacao = new MetaAdsPreparacaoPublicacao
            {
                Id = Guid.NewGuid(),
                CampanhaId = campanha.Id,
                DataCriacao = now
            };
            await preparacaoRepository.AdicionarAsync(preparacao, cancellationToken);
        }

        preparacao.MetaAdsContaId = conta.Id;
        preparacao.AdAccountId = selecao.AdAccountId;
        preparacao.LocationKey = location.Key;
        preparacao.LocationName = location.Name;
        preparacao.LocationType = location.Type;
        preparacao.CountryCode = location.CountryCode;
        preparacao.CountryName = location.CountryName;
        preparacao.Region = location.Region;
        preparacao.RegionId = location.RegionId;
        preparacao.AgeMin = ageMin;
        preparacao.AgeMax = ageMax;
        preparacao.DataAtualizacao = now;
        await preparacaoRepository.SalvarAsync(cancellationToken);
        return location;
    }

    public async Task<MetaAdsUploadImageResponse> EnviarImagemAsync(Guid campanhaId, string nomeArquivo, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        if (content.Length == 0)
        {
            throw new ArgumentException("Imagem obrigatoria.");
        }
        if (string.IsNullOrWhiteSpace(nomeArquivo))
        {
            throw new ArgumentException("Nome da imagem obrigatorio.");
        }
        if (!SupportedImageTypes.Contains(contentType))
        {
            throw new ArgumentException("Formato de imagem nao suportado para Meta Ads.");
        }

        var campanha = await campanhaRepository.ObterPorIdAsync(campanhaId, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");
        var (conta, token, config) = await ContextAsync(cancellationToken);
        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken)
            ?? throw new InvalidOperationException("Selecione os ativos Meta antes de enviar a imagem.");
        if (string.IsNullOrWhiteSpace(selecao.BusinessId) || string.IsNullOrWhiteSpace(selecao.AdAccountId))
        {
            throw new InvalidOperationException("Selecione Business e Ad Account antes de enviar a imagem.");
        }

        var adAccount = (await graphClient.ListAdAccountsAsync(config, token, selecao.BusinessId, cancellationToken))
            .FirstOrDefault(x => Same(x.Id, selecao.AdAccountId) || Same(x.AccountId, selecao.AdAccountId));
        if (adAccount is null)
        {
            throw new InvalidOperationException("Ad Account selecionada nao esta acessivel com a conexao Meta atual.");
        }

        await EnsureAdsManagementAsync(config, token, cancellationToken);
        var contentHash = Convert.ToHexString(SHA256.HashData(content));
        var existing = await imagemRepository.ObterPorConteudoAsync(campanha.Id, adAccount.Id, contentHash, cancellationToken);
        if (existing is not null && !string.IsNullOrWhiteSpace(existing.MetaImageHash))
        {
            return ToResponse(existing, true, "Imagem ja enviada anteriormente para esta Ad Account.");
        }

        var imageHash = await graphClient.UploadAdImageAsync(config, token, adAccount.Id, nomeArquivo, contentType, content, cancellationToken);
        var now = DateTime.UtcNow;
        var imagem = new MetaAdsImagem
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanha.Id,
            MetaAdsContaId = conta.Id,
            AdAccountId = adAccount.Id,
            OrigemImagem = "UploadUsuario",
            NomeArquivo = nomeArquivo,
            ContentType = contentType,
            TamanhoBytes = content.LongLength,
            ContentHash = contentHash,
            MetaImageHash = imageHash,
            DataUpload = now,
            DataAtualizacao = now
        };
        await imagemRepository.AdicionarAsync(imagem, cancellationToken);
        await imagemRepository.SalvarAsync(cancellationToken);
        return ToResponse(imagem, false, "Imagem enviada para a Ad Account Meta.");
    }

    private async Task EnsureAdsManagementAsync(MetaAdsConfiguration config, string token, CancellationToken cancellationToken)
    {
        var permissions = await graphClient.GetPermissionsAsync(config, token, cancellationToken);
        if (!permissions.Granted.Contains("ads_management", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Permissao ads_management necessaria para enviar imagem e publicar anuncios.");
        }
    }

    private async Task<(MetaAdsConta Conta, string Token, MetaAdsConfiguration Config)> ContextAsync(CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterAtivaAsync(cancellationToken)
            ?? throw new InvalidOperationException("Meta Ads nao conectado.");
        if (string.IsNullOrWhiteSpace(conta.AccessTokenProtegido))
        {
            throw new InvalidOperationException("Reconecte Meta Ads antes de preparar publicacao.");
        }
        if (conta.AccessTokenExpiraEm is not null && conta.AccessTokenExpiraEm <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Token Meta expirado. Reconecte Meta Ads.");
        }

        return (conta, protector.Unprotect(conta.AccessTokenProtegido), await Config(cancellationToken));
    }

    private async Task<MetaAdsConfiguration> Config(CancellationToken cancellationToken)
    {
        return new MetaAdsConfiguration(
            await Value(CategoriaConfiguracao.MetaAds, "AppId", cancellationToken),
            await Value(CategoriaConfiguracao.MetaAds, "AppSecret", cancellationToken),
            await Value(CategoriaConfiguracao.MetaAds, "RedirectUri", cancellationToken) ?? string.Empty,
            await Value(CategoriaConfiguracao.MetaAds, "AuthEndpoint", cancellationToken) ?? string.Empty,
            await Value(CategoriaConfiguracao.MetaAds, "TokenEndpoint", cancellationToken) ?? string.Empty,
            await Value(CategoriaConfiguracao.MetaAds, "UserInfoEndpoint", cancellationToken) ?? string.Empty,
            await Value(CategoriaConfiguracao.MetaAds, "GraphApiBaseUrl", cancellationToken) ?? string.Empty,
            await Value(CategoriaConfiguracao.MetaAds, "GraphApiVersion", cancellationToken) ?? string.Empty,
            await Value(CategoriaConfiguracao.MetaAds, "Scopes", cancellationToken) ?? string.Empty);
    }

    private async Task<string> DefaultCountryAsync(CancellationToken cancellationToken)
    {
        return (await Value(CategoriaConfiguracao.MetaAds, "DefaultCountryCode", cancellationToken))?.Trim().ToUpperInvariant() ?? "BR";
    }

    private async Task<string?> Value(CategoriaConfiguracao categoria, string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(categoria, key, cancellationToken)).Value;
    }

    private static MetaAdsUploadImageResponse ToResponse(MetaAdsImagem imagem, bool reutilizado, string mensagem)
    {
        return new MetaAdsUploadImageResponse(true, imagem.Id, imagem.NomeArquivo, imagem.ContentType, imagem.TamanhoBytes, imagem.ContentHash, imagem.MetaImageHash, reutilizado, imagem.DataUpload, mensagem);
    }

    private static bool Same(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
