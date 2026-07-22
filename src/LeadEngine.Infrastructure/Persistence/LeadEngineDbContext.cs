using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class LeadEngineDbContext(DbContextOptions<LeadEngineDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<OrigemLead> OrigensLead => Set<OrigemLead>();
    public DbSet<TentativaCapturaLead> TentativasCapturaLead => Set<TentativaCapturaLead>();
    public DbSet<LogIntegracaoLead> LogsIntegracaoLead => Set<LogIntegracaoLead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tipo).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.WhatsApp).HasMaxLength(20).IsRequired();
            entity.Property(x => x.WhatsAppNormalizado).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.EmailNormalizado).HasMaxLength(180);
            entity.Property(x => x.Cep).HasMaxLength(8);
            entity.Property(x => x.Cidade).HasMaxLength(120);
            entity.Property(x => x.Uf).HasMaxLength(2);
            entity.Property(x => x.IdadesJson).HasColumnType("json");
            entity.Property(x => x.HospitalDesejado).HasMaxLength(120);
            entity.Property(x => x.OperadoraDesejada).HasMaxLength(120);
            entity.Property(x => x.PlanoAtual).HasMaxLength(120);
            entity.Property(x => x.NomeEmpresa).HasMaxLength(180);
            entity.Property(x => x.Cnpj).HasMaxLength(14);
            entity.Property(x => x.CnpjNormalizado).HasMaxLength(14);
            entity.Property(x => x.TextoConsentimentoVersao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ErroEnvio).HasMaxLength(500);
            entity.HasIndex(x => x.WhatsAppNormalizado);
            entity.HasIndex(x => x.CriadoEm);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Tipo);
        });

        modelBuilder.Entity<OrigemLead>(entity =>
        {
            entity.ToTable("OrigensLead");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Gclid).HasMaxLength(180);
            entity.Property(x => x.Gbraid).HasMaxLength(180);
            entity.Property(x => x.Wbraid).HasMaxLength(180);
            entity.Property(x => x.UtmSource).HasMaxLength(100);
            entity.Property(x => x.UtmMedium).HasMaxLength(100);
            entity.Property(x => x.UtmCampaign).HasMaxLength(180);
            entity.Property(x => x.UtmContent).HasMaxLength(180);
            entity.Property(x => x.UtmTerm).HasMaxLength(180);
            entity.Property(x => x.CampaignId).HasMaxLength(80);
            entity.Property(x => x.AdGroupId).HasMaxLength(80);
            entity.Property(x => x.AdId).HasMaxLength(80);
            entity.Property(x => x.Keyword).HasMaxLength(180);
            entity.Property(x => x.MatchType).HasMaxLength(80);
            entity.Property(x => x.Device).HasMaxLength(80);
            entity.Property(x => x.LandingPage).HasMaxLength(250);
            entity.Property(x => x.Referrer).HasMaxLength(500);
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.IpHash).HasMaxLength(128);
            entity.HasIndex(x => x.Gclid);
            entity.HasIndex(x => x.UtmCampaign);
            entity.HasIndex(x => x.LandingPage);
            entity.HasIndex(x => x.LeadId).IsUnique();
            entity.HasOne(x => x.Lead)
                .WithOne(x => x.Origem)
                .HasForeignKey<OrigemLead>(x => x.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TentativaCapturaLead>(entity =>
        {
            entity.ToTable("TentativasCapturaLead");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WhatsAppNormalizado).HasMaxLength(20).IsRequired();
            entity.Property(x => x.LandingPage).HasMaxLength(250);
            entity.Property(x => x.UtmCampaign).HasMaxLength(180);
            entity.Property(x => x.Gclid).HasMaxLength(180);
            entity.HasIndex(x => x.WhatsAppNormalizado);
            entity.HasIndex(x => x.CriadoEm);
            entity.HasIndex(x => x.LeadId);
            entity.HasOne(x => x.Lead)
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LogIntegracaoLead>(entity =>
        {
            entity.ToTable("LogsIntegracaoLead");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Mensagem).HasMaxLength(500);
            entity.Property(x => x.Endpoint).HasMaxLength(250);
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.CriadoEm);
            entity.HasOne(x => x.Lead)
                .WithMany(x => x.LogsIntegracao)
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
