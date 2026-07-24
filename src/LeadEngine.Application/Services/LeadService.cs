using System.Net.Mail;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Services;

public sealed class LeadService(
    ICampanhaRepository campanhaRepository,
    ILeadRepository leadRepository,
    IWhatsAppUrlBuilder whatsAppUrlBuilder,
    IRequestContext requestContext,
    IOptions<LeadCaptureOptions> options) : ILeadService
{
    public async Task<CampanhaPublicaResponse?> ObterCampanhaPublicaAsync(string slug, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPublicadaPorSlugAsync(NormalizeSlug(slug), cancellationToken);
        return campanha is null ? null : ToPublicResponse(campanha);
    }

    public async Task<CapturarLeadPublicoResponse> CapturarLeadPublicoAsync(string slug, CapturarLeadPublicoRequest request, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPublicadaPorSlugAsync(NormalizeSlug(slug), cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");

        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return new CapturarLeadPublicoResponse(Guid.Empty, "Lead registrado com sucesso.", whatsAppUrlBuilder.Build(FakeLead(request), campanha));
        }

        ValidateAntiSpam(request);
        Validate(request);

        var telefone = LeadSanitizer.Digitos(request.Telefone);
        var janela = DateTime.UtcNow.AddHours(-Math.Max(1, options.Value.DuplicateWindowHours));
        var duplicado = await leadRepository.ObterDuplicadoRecenteAsync(campanha.Id, telefone, janela, cancellationToken);
        if (duplicado is not null)
        {
            return new CapturarLeadPublicoResponse(duplicado.Id, "Lead registrado com sucesso.", whatsAppUrlBuilder.Build(duplicado, campanha));
        }

        var lead = CriarLead(campanha, request, telefone);
        await leadRepository.AdicionarAsync(lead, cancellationToken);
        await leadRepository.SalvarAsync(cancellationToken);

        return new CapturarLeadPublicoResponse(lead.Id, "Lead registrado com sucesso.", whatsAppUrlBuilder.Build(lead, campanha));
    }

    private Lead CriarLead(Campanha campanha, CapturarLeadPublicoRequest request, string telefone)
    {
        var now = DateTime.UtcNow;
        var email = LeadSanitizer.Email(request.Email);
        var estado = LeadSanitizer.Texto(request.Estado, 2)!.ToUpperInvariant();
        var origemLanding = $"/lp/{campanha.Slug}";

        return new Lead
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanha.Id,
            Tipo = ToTipoLead(request.TipoContratacao),
            TipoContratacao = request.TipoContratacao,
            Nome = LeadSanitizer.Texto(request.Nome, 120)!,
            WhatsApp = telefone,
            WhatsAppNormalizado = telefone,
            Email = email,
            EmailNormalizado = email,
            Cidade = LeadSanitizer.Texto(request.Cidade, 100),
            Uf = estado,
            QuantidadeVidas = request.QuantidadeVidas,
            Observacao = LeadSanitizer.Texto(request.Observacao, 1000),
            Status = StatusLead.Recebido,
            ConsentimentoContato = true,
            ConsentimentoEm = now,
            TextoConsentimentoVersao = options.Value.ConsentVersion,
            CriadoEm = now,
            OrigemCaptura = "LandingPage",
            IpHash = requestContext.IpHash,
            UserAgentResumo = LeadSanitizer.Texto(requestContext.UserAgent, 300),
            UtmSource = LeadSanitizer.Texto(request.UtmSource, 100),
            UtmMedium = LeadSanitizer.Texto(request.UtmMedium, 100),
            UtmCampaign = LeadSanitizer.Texto(request.UtmCampaign, 180),
            UtmTerm = LeadSanitizer.Texto(request.UtmTerm, 180),
            UtmContent = LeadSanitizer.Texto(request.UtmContent, 180),
            Gclid = LeadSanitizer.Texto(request.Gclid, 180),
            Fbclid = LeadSanitizer.Texto(request.Fbclid, 180),
            StatusEnvioExterno = "Pendente",
            TentativasEnvioExterno = 0,
            Origem = new OrigemLead
            {
                Id = Guid.NewGuid(),
                UtmSource = LeadSanitizer.Texto(request.UtmSource, 100),
                UtmMedium = LeadSanitizer.Texto(request.UtmMedium, 100),
                UtmCampaign = LeadSanitizer.Texto(request.UtmCampaign, 180),
                UtmTerm = LeadSanitizer.Texto(request.UtmTerm, 180),
                UtmContent = LeadSanitizer.Texto(request.UtmContent, 180),
                Gclid = LeadSanitizer.Texto(request.Gclid, 180),
                LandingPage = origemLanding,
                UserAgent = LeadSanitizer.Texto(requestContext.UserAgent, 500),
                IpHash = requestContext.IpHash
            }
        };
    }

    private static CampanhaPublicaResponse ToPublicResponse(Campanha campanha)
    {
        var snapshot = CampanhaContentSnapshot.From(campanha);
        return new CampanhaPublicaResponse(
            campanha.Nome,
            snapshot.TituloLandingPage,
            snapshot.SubtituloLandingPage,
            snapshot.TextoBotao,
            snapshot.Beneficios,
            snapshot.PerguntasFrequentes.Select(x => new FaqResponse(x.Pergunta, x.Resposta)).ToArray(),
            campanha.Operadora,
            campanha.Cidade,
            campanha.Estado,
            campanha.TipoPublico,
            snapshot.MensagemWhatsApp);
    }

    private void ValidateAntiSpam(CapturarLeadPublicoRequest request)
    {
        if (request.FormOpenedAt is null)
        {
            throw new ArgumentException("Nao foi possivel registrar a solicitacao.");
        }

        var opened = DateTimeOffset.FromUnixTimeMilliseconds(request.FormOpenedAt.Value);
        if (DateTimeOffset.UtcNow - opened < TimeSpan.FromSeconds(Math.Max(0, options.Value.MinimumFormSeconds)))
        {
            throw new ArgumentException("Nao foi possivel registrar a solicitacao.");
        }

        if (string.IsNullOrWhiteSpace(requestContext.UserAgent))
        {
            throw new ArgumentException("Nao foi possivel registrar a solicitacao.");
        }
    }

    private static void Validate(CapturarLeadPublicoRequest request)
    {
        var erros = new List<string>();
        var nome = LeadSanitizer.Texto(request.Nome, 120);
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 2)
        {
            erros.Add("Nome obrigatorio com pelo menos 2 caracteres.");
        }

        var telefone = LeadSanitizer.Digitos(request.Telefone);
        if (telefone.Length is < 10 or > 13)
        {
            erros.Add("Telefone invalido.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (request.Email.Length > 160)
            {
                erros.Add("Email deve ter no maximo 160 caracteres.");
            }
            else
            {
                try { _ = new MailAddress(request.Email); }
                catch { erros.Add("Email invalido."); }
            }
        }

        if (string.IsNullOrWhiteSpace(request.Cidade))
        {
            erros.Add("Cidade obrigatoria.");
        }

        if (request.Cidade.Length > 100)
        {
            erros.Add("Cidade deve ter no maximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Estado) || request.Estado.Trim().Length != 2)
        {
            erros.Add("Estado deve ter exatamente 2 caracteres.");
        }

        if (request.QuantidadeVidas is <= 0 or > 999)
        {
            erros.Add("Quantidade de vidas invalida.");
        }

        if (!Enum.IsDefined(request.TipoContratacao))
        {
            erros.Add("Tipo de contratacao invalido.");
        }

        if (!request.Consentimento)
        {
            erros.Add("Consentimento obrigatorio.");
        }

        if (request.Observacao?.Length > 1000)
        {
            erros.Add("Observacao deve ter no maximo 1000 caracteres.");
        }

        ValidateMax(request.UtmSource, 100, "utmSource", erros);
        ValidateMax(request.UtmMedium, 100, "utmMedium", erros);
        ValidateMax(request.UtmCampaign, 180, "utmCampaign", erros);
        ValidateMax(request.UtmTerm, 180, "utmTerm", erros);
        ValidateMax(request.UtmContent, 180, "utmContent", erros);
        ValidateMax(request.Gclid, 180, "gclid", erros);
        ValidateMax(request.Fbclid, 180, "fbclid", erros);

        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }
    }

    private static void ValidateMax(string? value, int max, string field, ICollection<string> erros)
    {
        if (value?.Length > max)
        {
            erros.Add($"{field} deve ter no maximo {max} caracteres.");
        }
    }

    private static TipoLead ToTipoLead(TipoContratacaoLead tipo)
    {
        return tipo switch
        {
            TipoContratacaoLead.Individual => TipoLead.PessoaFisica,
            TipoContratacaoLead.Familiar => TipoLead.Familia,
            TipoContratacaoLead.Mei => TipoLead.Mei,
            TipoContratacaoLead.Empresarial => TipoLead.Empresa,
            _ => TipoLead.PessoaFisica
        };
    }

    private static string NormalizeSlug(string slug)
    {
        return CampanhaText.Slugify(slug);
    }

    private static Lead FakeLead(CapturarLeadPublicoRequest request)
    {
        return new Lead
        {
            Id = Guid.Empty,
            Nome = LeadSanitizer.Texto(request.Nome, 120) ?? "Interessado",
            Cidade = LeadSanitizer.Texto(request.Cidade, 100),
            Uf = LeadSanitizer.Texto(request.Estado, 2)?.ToUpperInvariant(),
            QuantidadeVidas = request.QuantidadeVidas,
            TipoContratacao = request.TipoContratacao,
            Observacao = LeadSanitizer.Texto(request.Observacao, 1000)
        };
    }
}
