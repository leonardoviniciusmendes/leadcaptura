using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAdsMetricsOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataAtribuicao",
                table: "Leads",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GoogleAdsPublicacaoId",
                table: "Leads",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "TipoAtribuicao",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GoogleAdsAnalisesIa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PeriodoInicial = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFinal = table.Column<DateOnly>(type: "date", nullable: false),
                    Modelo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Resumo = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultadoJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokensEntrada = table.Column<int>(type: "int", nullable: true),
                    TokensSaida = table.Column<int>(type: "int", nullable: true),
                    CustoEstimado = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    DuracaoMs = table.Column<long>(type: "bigint", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Aplicada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataAplicacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsAnalisesIa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsAnalisesIa_GoogleAdsPublicacoes_GoogleAdsPublicacao~",
                        column: x => x.GoogleAdsPublicacaoId,
                        principalTable: "GoogleAdsPublicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GoogleAdsMetricasDiarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsContaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CampaignResourceName = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CampaignExternalId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Impressoes = table.Column<long>(type: "bigint", nullable: false),
                    Cliques = table.Column<long>(type: "bigint", nullable: false),
                    CustoMicros = table.Column<long>(type: "bigint", nullable: false),
                    Custo = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Ctr = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    CpcMedioMicros = table.Column<long>(type: "bigint", nullable: false),
                    CpcMedio = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Conversoes = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ValorConversoes = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TaxaConversao = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    ParcelaImpressoesPesquisa = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    TaxaTopoPagina = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    TaxaTopoAbsoluto = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: true),
                    DataSincronizacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsMetricasDiarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsMetricasDiarias_GoogleAdsContas_GoogleAdsContaId",
                        column: x => x.GoogleAdsContaId,
                        principalTable: "GoogleAdsContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoogleAdsMetricasDiarias_GoogleAdsPublicacoes_GoogleAdsPubli~",
                        column: x => x.GoogleAdsPublicacaoId,
                        principalTable: "GoogleAdsPublicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GoogleAdsSincronizacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    GoogleAdsContaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataConclusao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PeriodoInicial = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodoFinal = table.Column<DateOnly>(type: "date", nullable: true),
                    RegistrosConsultados = table.Column<int>(type: "int", nullable: false),
                    RegistrosCriados = table.Column<int>(type: "int", nullable: false),
                    RegistrosAtualizados = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroCodigo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroMensagemControlada = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DuracaoMs = table.Column<long>(type: "bigint", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsSincronizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsSincronizacoes_GoogleAdsContas_GoogleAdsContaId",
                        column: x => x.GoogleAdsContaId,
                        principalTable: "GoogleAdsContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoogleAdsSincronizacoes_GoogleAdsPublicacoes_GoogleAdsPublic~",
                        column: x => x.GoogleAdsPublicacaoId,
                        principalTable: "GoogleAdsPublicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_GoogleAdsPublicacaoId",
                table: "Leads",
                column: "GoogleAdsPublicacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TipoAtribuicao",
                table: "Leads",
                column: "TipoAtribuicao");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsAnalisesIa_DataCriacao",
                table: "GoogleAdsAnalisesIa",
                column: "DataCriacao");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsAnalisesIa_GoogleAdsPublicacaoId",
                table: "GoogleAdsAnalisesIa",
                column: "GoogleAdsPublicacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsMetricasDiarias_Data",
                table: "GoogleAdsMetricasDiarias",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsMetricasDiarias_GoogleAdsContaId",
                table: "GoogleAdsMetricasDiarias",
                column: "GoogleAdsContaId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsMetricasDiarias_GoogleAdsPublicacaoId_CampaignExter~",
                table: "GoogleAdsMetricasDiarias",
                columns: new[] { "GoogleAdsPublicacaoId", "CampaignExternalId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsSincronizacoes_DataInicio",
                table: "GoogleAdsSincronizacoes",
                column: "DataInicio");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsSincronizacoes_GoogleAdsContaId",
                table: "GoogleAdsSincronizacoes",
                column: "GoogleAdsContaId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsSincronizacoes_GoogleAdsPublicacaoId",
                table: "GoogleAdsSincronizacoes",
                column: "GoogleAdsPublicacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAdsAnalisesIa");

            migrationBuilder.DropTable(
                name: "GoogleAdsMetricasDiarias");

            migrationBuilder.DropTable(
                name: "GoogleAdsSincronizacoes");

            migrationBuilder.DropIndex(
                name: "IX_Leads_GoogleAdsPublicacaoId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TipoAtribuicao",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "DataAtribuicao",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "GoogleAdsPublicacaoId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "TipoAtribuicao",
                table: "Leads");
        }
    }
}
