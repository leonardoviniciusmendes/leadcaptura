using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampaignReviewService(
    ICampanhaRepository repository,
    ICampaignSectionGenerationService sectionGenerationService) : ICampaignReviewService
{
    public async Task<CampanhaResponse?> ObterRevisaoAsync(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await repository.ObterPorIdAsync(id, cancellationToken);
        return campanha is null ? null : CampanhaMapping.ToResponse(campanha);
    }

    public async Task<CampanhaResponse> RevisarCampanhaAsync(Guid id, RevisarCampanhaRequest request, CancellationToken cancellationToken)
    {
        CampanhaValidator.ValidarRevisao(request);
        var campanha = await ObterCampanhaAsync(id, cancellationToken);
        if (campanha.Status == StatusCampanha.Gerando)
        {
            throw new ArgumentException("Campanha em geracao nao pode ser revisada.");
        }

        var anterior = CampanhaContentSnapshot.From(campanha);
        var conteudo = CampanhaValidator.NormalizarEValidarConteudo(
            request.TituloLandingPage,
            request.SubtituloLandingPage,
            request.TextoBotao,
            request.MensagemWhatsApp,
            request.Beneficios,
            request.PerguntasFrequentes.Select(x => new FaqItemValidation(x.Pergunta, x.Resposta)),
            request.PalavrasChave,
            request.PalavrasChaveNegativas,
            request.TitulosAnuncios,
            request.DescricoesAnuncios);

        campanha.Nome = CampanhaText.Limitar(request.Nome, 180) ?? string.Empty;
        ApplyContent(campanha, conteudo);
        campanha.Status = StatusCampanha.Gerada;
        campanha.DataAtualizacao = DateTime.UtcNow;

        await RegistrarAsync(campanha.Id, "Edicao manual", null, anterior, CampanhaContentSnapshot.From(campanha), OrigemRevisaoCampanha.Manual, null, null, null, cancellationToken);
        await repository.SalvarAsync(cancellationToken);
        return CampanhaMapping.ToResponse(campanha);
    }

    public async Task<CampanhaResponse> RegenerarSecaoAsync(Guid id, RegenerarCampanhaSecaoRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Secao))
        {
            throw new ArgumentException("Secao invalida.");
        }

        var campanha = await ObterCampanhaAsync(id, cancellationToken);
        if (campanha.Status == StatusCampanha.Gerando)
        {
            throw new ArgumentException("Campanha em geracao nao pode ser regenerada.");
        }

        var anterior = CampanhaContentSnapshot.From(campanha);
        var result = await sectionGenerationService.GenerateAsync(campanha, request.Secao, CampanhaText.Limitar(request.InstrucaoAdicional, 500), cancellationToken);
        if (result.Secao != request.Secao)
        {
            throw new CampaignGenerationException("Provider retornou secao diferente da solicitada.");
        }

        try
        {
            ApplySection(campanha, result.Secao, result.Conteudo);
            ValidateCurrent(campanha);
        }
        catch
        {
            ApplySnapshot(campanha, anterior);
            throw;
        }

        campanha.Status = StatusCampanha.Gerada;
        campanha.DataAtualizacao = DateTime.UtcNow;

        await RegistrarAsync(campanha.Id, "Regeneracao parcial", result.Secao, anterior, CampanhaContentSnapshot.From(campanha), OrigemRevisaoCampanha.InteligenciaArtificial, request.InstrucaoAdicional, result.Provider, result.Modelo, cancellationToken);
        await repository.SalvarAsync(cancellationToken);
        return CampanhaMapping.ToResponse(campanha);
    }

    public async Task<CampanhaResponse> AprovarCampanhaAsync(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await ObterCampanhaAsync(id, cancellationToken);
        if (campanha.Status is StatusCampanha.Gerando or StatusCampanha.Erro)
        {
            throw new ArgumentException("Campanha com status Gerando ou Erro nao pode ser aprovada.");
        }

        ValidateCurrent(campanha);
        var anterior = CampanhaContentSnapshot.From(campanha);
        campanha.Status = StatusCampanha.Revisada;
        campanha.DataAtualizacao = DateTime.UtcNow;
        await RegistrarAsync(campanha.Id, "Aprovacao", null, anterior, CampanhaContentSnapshot.From(campanha), OrigemRevisaoCampanha.Manual, null, null, null, cancellationToken);
        await repository.SalvarAsync(cancellationToken);
        return CampanhaMapping.ToResponse(campanha);
    }

    public async Task<IReadOnlyList<CampanhaRevisaoHistoricoResponse>> ListarHistoricoAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await ObterCampanhaAsync(id, cancellationToken);
        var revisoes = await repository.ListarRevisoesAsync(id, cancellationToken);
        return revisoes
            .OrderByDescending(x => x.DataAlteracao)
            .Select(x => new CampanhaRevisaoHistoricoResponse(
                x.DataAlteracao,
                x.Secao,
                x.Origem,
                Resumo(x),
                x.ProviderIa,
                x.ModeloIa))
            .ToArray();
    }

    private async Task<Campanha> ObterCampanhaAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");
    }

    private static void ApplySection(Campanha campanha, CampanhaSecao secao, object conteudo)
    {
        switch (secao)
        {
            case CampanhaSecao.Nome:
                campanha.Nome = CampanhaText.Limitar((string)conteudo, 180) ?? string.Empty;
                break;
            case CampanhaSecao.LandingPage:
                var landing = (LandingPageSection)conteudo;
                campanha.TituloLandingPage = landing.TituloLandingPage;
                campanha.SubtituloLandingPage = landing.SubtituloLandingPage;
                campanha.TextoBotao = landing.TextoBotao;
                break;
            case CampanhaSecao.MensagemWhatsApp:
                campanha.MensagemWhatsApp = (string)conteudo;
                break;
            case CampanhaSecao.Beneficios:
                campanha.BeneficiosJson = Serialize((IReadOnlyList<string>)conteudo);
                break;
            case CampanhaSecao.PerguntasFrequentes:
                campanha.PerguntasFrequentesJson = Serialize(((IReadOnlyList<FaqItemValidation>)conteudo).Select(x => new FaqResponse(x.Pergunta, x.Resposta)).ToArray());
                break;
            case CampanhaSecao.PalavrasChave:
                campanha.PalavrasChaveJson = Serialize((IReadOnlyList<string>)conteudo);
                break;
            case CampanhaSecao.PalavrasChaveNegativas:
                campanha.PalavrasChaveNegativasJson = Serialize((IReadOnlyList<string>)conteudo);
                break;
            case CampanhaSecao.TitulosAnuncios:
                campanha.TitulosAnunciosJson = Serialize((IReadOnlyList<string>)conteudo);
                break;
            case CampanhaSecao.DescricoesAnuncios:
                campanha.DescricoesAnunciosJson = Serialize((IReadOnlyList<string>)conteudo);
                break;
            default:
                throw new ArgumentException("Secao invalida.");
        }
    }

    private static void ApplyContent(Campanha campanha, CampanhaConteudoNormalizado conteudo)
    {
        campanha.TituloLandingPage = conteudo.TituloLandingPage;
        campanha.SubtituloLandingPage = conteudo.SubtituloLandingPage;
        campanha.TextoBotao = conteudo.TextoBotao;
        campanha.MensagemWhatsApp = conteudo.MensagemWhatsApp;
        campanha.BeneficiosJson = Serialize(conteudo.Beneficios);
        campanha.PerguntasFrequentesJson = Serialize(conteudo.PerguntasFrequentes.Select(x => new FaqResponse(x.Pergunta, x.Resposta)).ToArray());
        campanha.PalavrasChaveJson = Serialize(conteudo.PalavrasChave);
        campanha.PalavrasChaveNegativasJson = Serialize(conteudo.PalavrasChaveNegativas);
        campanha.TitulosAnunciosJson = Serialize(conteudo.TitulosAnuncios);
        campanha.DescricoesAnunciosJson = Serialize(conteudo.DescricoesAnuncios);
    }

    private static void ApplySnapshot(Campanha campanha, CampanhaContentSnapshot snapshot)
    {
        campanha.Nome = snapshot.Nome;
        campanha.TituloLandingPage = snapshot.TituloLandingPage;
        campanha.SubtituloLandingPage = snapshot.SubtituloLandingPage;
        campanha.TextoBotao = snapshot.TextoBotao;
        campanha.MensagemWhatsApp = snapshot.MensagemWhatsApp;
        campanha.BeneficiosJson = Serialize(snapshot.Beneficios);
        campanha.PerguntasFrequentesJson = Serialize(snapshot.PerguntasFrequentes.Select(x => new FaqResponse(x.Pergunta, x.Resposta)).ToArray());
        campanha.PalavrasChaveJson = Serialize(snapshot.PalavrasChave);
        campanha.PalavrasChaveNegativasJson = Serialize(snapshot.PalavrasChaveNegativas);
        campanha.TitulosAnunciosJson = Serialize(snapshot.TitulosAnuncios);
        campanha.DescricoesAnunciosJson = Serialize(snapshot.DescricoesAnuncios);
    }

    private static void ValidateCurrent(Campanha campanha)
    {
        var atual = CampanhaContentSnapshot.From(campanha);
        CampanhaValidator.ValidarCampanhaCompleta(new CampanhaConteudoNormalizado(
            atual.TituloLandingPage,
            atual.SubtituloLandingPage,
            atual.TextoBotao,
            atual.MensagemWhatsApp,
            atual.Beneficios,
            atual.PerguntasFrequentes.Select(x => new FaqItemValidation(x.Pergunta, x.Resposta)).ToArray(),
            atual.PalavrasChave,
            atual.PalavrasChaveNegativas,
            atual.TitulosAnuncios,
            atual.DescricoesAnuncios));

        if (string.IsNullOrWhiteSpace(campanha.Nome))
        {
            throw new ArgumentException("Nome obrigatorio.");
        }
    }

    private async Task RegistrarAsync(
        Guid campanhaId,
        string tipoAlteracao,
        CampanhaSecao? secao,
        object anterior,
        object novo,
        OrigemRevisaoCampanha origem,
        string? instrucaoAdicional,
        string? provider,
        string? modelo,
        CancellationToken cancellationToken)
    {
        await repository.AdicionarRevisaoAsync(new CampanhaRevisao
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanhaId,
            TipoAlteracao = tipoAlteracao,
            Secao = secao,
            ConteudoAnterior = JsonSerializer.Serialize(anterior),
            ConteudoNovo = JsonSerializer.Serialize(novo),
            Origem = origem,
            InstrucaoAdicional = CampanhaText.Limitar(instrucaoAdicional, 500),
            ProviderIa = provider,
            ModeloIa = modelo,
            DataAlteracao = DateTime.UtcNow
        }, cancellationToken);
    }

    private static string Resumo(CampanhaRevisao revisao)
    {
        return revisao.Secao is null
            ? revisao.TipoAlteracao
            : $"{revisao.TipoAlteracao}: {revisao.Secao}";
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }
}
