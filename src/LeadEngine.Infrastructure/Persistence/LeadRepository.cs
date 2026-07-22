using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class LeadRepository(LeadEngineDbContext context) : ILeadRepository
{
    public Task<Lead?> ObterDuplicadoRecenteAsync(string whatsAppNormalizado, DateTime criadoApos, CancellationToken cancellationToken)
    {
        return context.Leads
            .Include(x => x.Origem)
            .Where(x => x.WhatsAppNormalizado == whatsAppNormalizado && x.CriadoEm >= criadoApos)
            .OrderByDescending(x => x.CriadoEm)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Lead?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Leads
            .Include(x => x.Origem)
            .Include(x => x.LogsIntegracao)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Lead>> ListarAsync(LeadQuery query, CancellationToken cancellationToken)
    {
        var pagina = Math.Max(query.Pagina, 1);
        var tamanhoPagina = Math.Clamp(query.TamanhoPagina, 1, 100);
        var leads = context.Leads.Include(x => x.Origem).AsQueryable();

        if (query.DataInicial is not null)
        {
            leads = leads.Where(x => x.CriadoEm >= query.DataInicial.Value);
        }

        if (query.DataFinal is not null)
        {
            leads = leads.Where(x => x.CriadoEm <= query.DataFinal.Value);
        }

        if (query.Tipo is not null)
        {
            leads = leads.Where(x => x.Tipo == query.Tipo.Value);
        }

        if (query.Status is not null)
        {
            leads = leads.Where(x => x.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Campanha))
        {
            leads = leads.Where(x => x.Origem.UtmCampaign == query.Campanha);
        }

        if (!string.IsNullOrWhiteSpace(query.LandingPage))
        {
            leads = leads.Where(x => x.Origem.LandingPage == query.LandingPage);
        }

        var whatsapp = LeadSanitizer.Digitos(query.WhatsApp);
        if (!string.IsNullOrWhiteSpace(whatsapp))
        {
            leads = leads.Where(x => x.WhatsAppNormalizado == whatsapp);
        }

        var total = await leads.CountAsync(cancellationToken);
        var itens = await leads
            .OrderByDescending(x => x.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Lead>(itens, total, pagina, tamanhoPagina);
    }

    public Task AdicionarAsync(Lead lead, CancellationToken cancellationToken)
    {
        return context.Leads.AddAsync(lead, cancellationToken).AsTask();
    }

    public Task AdicionarTentativaAsync(TentativaCapturaLead tentativa, CancellationToken cancellationToken)
    {
        return context.TentativasCapturaLead.AddAsync(tentativa, cancellationToken).AsTask();
    }

    public Task AdicionarLogIntegracaoAsync(LogIntegracaoLead log, CancellationToken cancellationToken)
    {
        return context.LogsIntegracaoLead.AddAsync(log, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
