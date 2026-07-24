using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class LeadEngineDbContext(DbContextOptions<LeadEngineDbContext> options) : DbContext(options)
{
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<CampanhaRevisao> CampanhasRevisoes => Set<CampanhaRevisao>();
    public DbSet<ConfiguracaoSistema> ConfiguracoesSistema => Set<ConfiguracaoSistema>();
    public DbSet<ConfiguracaoSistemaHistorico> ConfiguracoesSistemaHistorico => Set<ConfiguracaoSistemaHistorico>();
    public DbSet<GoogleAdsConta> GoogleAdsContas => Set<GoogleAdsConta>();
    public DbSet<GoogleAdsOAuthState> GoogleAdsOAuthStates => Set<GoogleAdsOAuthState>();
    public DbSet<GoogleAdsPlanoPublicacao> GoogleAdsPlanosPublicacao => Set<GoogleAdsPlanoPublicacao>();
    public DbSet<GoogleAdsPublicacao> GoogleAdsPublicacoes => Set<GoogleAdsPublicacao>();
    public DbSet<GoogleAdsRecursoPublicado> GoogleAdsRecursosPublicados => Set<GoogleAdsRecursoPublicado>();
    public DbSet<GoogleAdsPublicacaoHistorico> GoogleAdsPublicacaoHistoricos => Set<GoogleAdsPublicacaoHistorico>();
    public DbSet<GoogleAdsOperacaoPublicacao> GoogleAdsOperacoesPublicacao => Set<GoogleAdsOperacaoPublicacao>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<OrigemLead> OrigensLead => Set<OrigemLead>();
    public DbSet<TentativaCapturaLead> TentativasCapturaLead => Set<TentativaCapturaLead>();
    public DbSet<LogIntegracaoLead> LogsIntegracaoLead => Set<LogIntegracaoLead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campanha>(entity =>
        {
            entity.ToTable("Campanhas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TipoPublico).HasConversion<int>();
            entity.Property(x => x.Cidade).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Estado).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Regiao).HasMaxLength(120);
            entity.Property(x => x.Operadora).HasMaxLength(80).IsRequired();
            entity.Property(x => x.OrcamentoDiario).HasPrecision(10, 2);
            entity.Property(x => x.Objetivo).HasMaxLength(500);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.TituloLandingPage).HasMaxLength(180).IsRequired();
            entity.Property(x => x.SubtituloLandingPage).HasMaxLength(300).IsRequired();
            entity.Property(x => x.TextoBotao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MensagemWhatsApp).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            entity.Property(x => x.BeneficiosJson).HasColumnType("json");
            entity.Property(x => x.PerguntasFrequentesJson).HasColumnType("json");
            entity.Property(x => x.PalavrasChaveJson).HasColumnType("json");
            entity.Property(x => x.PalavrasChaveNegativasJson).HasColumnType("json");
            entity.Property(x => x.TitulosAnunciosJson).HasColumnType("json");
            entity.Property(x => x.DescricoesAnunciosJson).HasColumnType("json");
            entity.Property(x => x.ErroGeracao).HasMaxLength(500);
            entity.Property(x => x.ProviderIa).HasMaxLength(40);
            entity.Property(x => x.ModeloIa).HasMaxLength(120);
            entity.Property(x => x.UrlPublica).HasMaxLength(250);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.DataCriacao);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.Publicada, x.Ativo });
        });

        modelBuilder.Entity<CampanhaRevisao>(entity =>
        {
            entity.ToTable("CampanhasRevisoes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TipoAlteracao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Secao).HasConversion<int>();
            entity.Property(x => x.ConteudoAnterior).HasColumnType("json").IsRequired();
            entity.Property(x => x.ConteudoNovo).HasColumnType("json").IsRequired();
            entity.Property(x => x.Origem).HasConversion<int>();
            entity.Property(x => x.InstrucaoAdicional).HasMaxLength(500);
            entity.Property(x => x.ProviderIa).HasMaxLength(40);
            entity.Property(x => x.ModeloIa).HasMaxLength(120);
            entity.HasIndex(x => x.CampanhaId);
            entity.HasIndex(x => x.DataAlteracao);
            entity.HasOne(x => x.Campanha)
                .WithMany(x => x.Revisoes)
                .HasForeignKey(x => x.CampanhaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfiguracaoSistema>(entity =>
        {
            entity.ToTable("ConfiguracoesSistema");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Categoria).HasConversion<int>();
            entity.Property(x => x.Valor).HasMaxLength(2000);
            entity.Property(x => x.ValorProtegido).HasMaxLength(4000);
            entity.Property(x => x.Descricao).HasMaxLength(500);
            entity.HasIndex(x => x.Chave).IsUnique();
            entity.HasIndex(x => x.Categoria);
        });

        modelBuilder.Entity<ConfiguracaoSistemaHistorico>(entity =>
        {
            entity.ToTable("ConfiguracoesSistemaHistorico");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Categoria).HasConversion<int>();
            entity.Property(x => x.ValorAnterior).HasMaxLength(2000);
            entity.Property(x => x.ValorNovo).HasMaxLength(2000);
            entity.Property(x => x.OrigemAlteracao).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Categoria);
            entity.HasIndex(x => x.Chave);
            entity.HasIndex(x => x.DataAlteracao);
            entity.HasOne(x => x.ConfiguracaoSistema)
                .WithMany(x => x.Historico)
                .HasForeignKey(x => x.ConfiguracaoSistemaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoogleAdsConta>(entity =>
        {
            entity.ToTable("GoogleAdsContas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.AccessTokenProtegido).HasMaxLength(4000);
            entity.Property(x => x.RefreshTokenProtegido).HasMaxLength(4000);
            entity.HasIndex(x => x.CustomerId).IsUnique();
            entity.HasIndex(x => x.Padrao);
            entity.HasIndex(x => x.Ativa);
        });

        modelBuilder.Entity<GoogleAdsOAuthState>(entity =>
        {
            entity.ToTable("GoogleAdsOAuthStates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StateHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.StateHash).IsUnique();
            entity.HasIndex(x => x.ExpiraEm);
            entity.HasIndex(x => x.Utilizado);
        });

        modelBuilder.Entity<GoogleAdsPlanoPublicacao>(entity =>
        {
            entity.ToTable("GoogleAdsPlanosPublicacao");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NomeCampanha).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Objetivo).HasMaxLength(500);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.TipoRede).HasMaxLength(30).IsRequired();
            entity.Property(x => x.OrcamentoDiario).HasPrecision(10, 2);
            entity.Property(x => x.CodigoMoeda).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Idioma).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Pais).HasMaxLength(10).IsRequired();
            entity.Property(x => x.UrlFinal).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ConteudoHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ErrosValidacaoJson).HasColumnType("json").IsRequired();
            entity.Property(x => x.AvisosValidacaoJson).HasColumnType("json").IsRequired();
            entity.Property(x => x.PayloadPreviewJson).HasColumnType("json").IsRequired();
            entity.HasIndex(x => x.CampanhaId).IsUnique();
            entity.HasIndex(x => x.GoogleAdsContaId);
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.Campanha)
                .WithMany()
                .HasForeignKey(x => x.CampanhaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.GoogleAdsConta)
                .WithMany()
                .HasForeignKey(x => x.GoogleAdsContaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoogleAdsPublicacao>(entity =>
        {
            entity.ToTable("GoogleAdsPublicacoes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PreviewHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(180).IsRequired();
            entity.Property(x => x.ConfirmationTokenHash).HasMaxLength(128);
            entity.Property(x => x.RequestIdValidacao).HasMaxLength(120);
            entity.Property(x => x.RequestIdPublicacao).HasMaxLength(120);
            entity.Property(x => x.ErroCodigo).HasMaxLength(120);
            entity.Property(x => x.ErroMensagemControlada).HasMaxLength(500);
            entity.Property(x => x.ErrosJson).HasColumnType("json").IsRequired();
            entity.Property(x => x.RecursosJson).HasColumnType("json").IsRequired();
            entity.Property(x => x.GeoTargetResourceName).HasMaxLength(80);
            entity.Property(x => x.LanguageResourceName).HasMaxLength(80);
            entity.Property(x => x.IsTestAccount);
            entity.HasIndex(x => new { x.GoogleAdsPlanoPublicacaoId, x.PreviewVersao, x.PreviewHash }).IsUnique();
            entity.HasIndex(x => x.CampanhaId);
            entity.HasIndex(x => x.GoogleAdsContaId);
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.PlanoPublicacao).WithMany().HasForeignKey(x => x.GoogleAdsPlanoPublicacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Campanha).WithMany().HasForeignKey(x => x.CampanhaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.GoogleAdsConta).WithMany().HasForeignKey(x => x.GoogleAdsContaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoogleAdsRecursoPublicado>(entity =>
        {
            entity.ToTable("GoogleAdsRecursosPublicados");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TipoRecurso).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ResourceName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ExternalId).HasMaxLength(120);
            entity.Property(x => x.Nome).HasMaxLength(180);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.GoogleAdsPublicacaoId);
            entity.HasIndex(x => x.ResourceName);
            entity.HasOne(x => x.Publicacao).WithMany(x => x.Recursos).HasForeignKey(x => x.GoogleAdsPublicacaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoogleAdsPublicacaoHistorico>(entity =>
        {
            entity.ToTable("GoogleAdsPublicacaoHistoricos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StatusAnterior).HasConversion<int?>();
            entity.Property(x => x.StatusNovo).HasConversion<int>();
            entity.Property(x => x.Operacao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MensagemControlada).HasMaxLength(500);
            entity.Property(x => x.RequestId).HasMaxLength(120);
            entity.Property(x => x.MetadadosJson).HasColumnType("json").IsRequired();
            entity.HasIndex(x => x.GoogleAdsPublicacaoId);
            entity.HasIndex(x => x.Data);
            entity.HasOne(x => x.Publicacao).WithMany(x => x.Historico).HasForeignKey(x => x.GoogleAdsPublicacaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoogleAdsOperacaoPublicacao>(entity =>
        {
            entity.ToTable("GoogleAdsOperacoesPublicacao");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TipoOperacao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntidadeOrigem).HasMaxLength(80);
            entity.Property(x => x.ResourceNameTemporario).HasMaxLength(300);
            entity.Property(x => x.ResourceNameDefinitivo).HasMaxLength(300);
            entity.Property(x => x.Status).HasMaxLength(60).IsRequired();
            entity.Property(x => x.CodigoErro).HasMaxLength(120);
            entity.Property(x => x.MensagemControlada).HasMaxLength(500);
            entity.HasIndex(x => x.GoogleAdsPublicacaoId);
            entity.HasIndex(x => new { x.GoogleAdsPublicacaoId, x.Indice });
            entity.HasOne(x => x.Publicacao).WithMany(x => x.Operacoes).HasForeignKey(x => x.GoogleAdsPublicacaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tipo).HasConversion<int>();
            entity.Property(x => x.TipoContratacao).HasConversion<int>();
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
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.Property(x => x.TextoConsentimentoVersao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.OrigemCaptura).HasMaxLength(40);
            entity.Property(x => x.IpHash).HasMaxLength(128);
            entity.Property(x => x.UserAgentResumo).HasMaxLength(300);
            entity.Property(x => x.UtmSource).HasMaxLength(100);
            entity.Property(x => x.UtmMedium).HasMaxLength(100);
            entity.Property(x => x.UtmCampaign).HasMaxLength(180);
            entity.Property(x => x.UtmTerm).HasMaxLength(180);
            entity.Property(x => x.UtmContent).HasMaxLength(180);
            entity.Property(x => x.Gclid).HasMaxLength(180);
            entity.Property(x => x.Fbclid).HasMaxLength(180);
            entity.Property(x => x.StatusEnvioExterno).HasMaxLength(40);
            entity.Property(x => x.UltimoErroEnvioExterno).HasMaxLength(500);
            entity.Property(x => x.ErroEnvio).HasMaxLength(500);
            entity.HasIndex(x => x.WhatsAppNormalizado);
            entity.HasIndex(x => x.CriadoEm);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Tipo);
            entity.HasIndex(x => x.CampanhaId);
            entity.HasIndex(x => new { x.CampanhaId, x.WhatsAppNormalizado, x.CriadoEm });
            entity.HasOne(x => x.Campanha)
                .WithMany(x => x.Leads)
                .HasForeignKey(x => x.CampanhaId)
                .OnDelete(DeleteBehavior.SetNull);
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
