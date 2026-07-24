using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAdsControlledPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleAdsPublicacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPlanoPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CampanhaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsContaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerId = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviewVersao = table.Column<int>(type: "int", nullable: false),
                    PreviewHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmationTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmationExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataPreparacao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataValidacaoRemota = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataInicioPublicacao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataConclusao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequestIdValidacao = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestIdPublicacao = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroCodigo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroMensagemControlada = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrosJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecursosJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    Teste = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GeoTargetResourceName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LanguageResourceName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsPublicacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsPublicacoes_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoogleAdsPublicacoes_GoogleAdsContas_GoogleAdsContaId",
                        column: x => x.GoogleAdsContaId,
                        principalTable: "GoogleAdsContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoogleAdsPublicacoes_GoogleAdsPlanosPublicacao_GoogleAdsPlan~",
                        column: x => x.GoogleAdsPlanoPublicacaoId,
                        principalTable: "GoogleAdsPlanosPublicacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GoogleAdsRecursosPublicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoRecurso = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceName = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsRecursosPublicados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsRecursosPublicados_GoogleAdsPublicacoes_GoogleAdsPu~",
                        column: x => x.GoogleAdsPublicacaoId,
                        principalTable: "GoogleAdsPublicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsPublicacoes_CampanhaId",
                table: "GoogleAdsPublicacoes",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsPublicacoes_GoogleAdsContaId",
                table: "GoogleAdsPublicacoes",
                column: "GoogleAdsContaId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsPublicacoes_GoogleAdsPlanoPublicacaoId_PreviewVersa~",
                table: "GoogleAdsPublicacoes",
                columns: new[] { "GoogleAdsPlanoPublicacaoId", "PreviewVersao", "PreviewHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsPublicacoes_Status",
                table: "GoogleAdsPublicacoes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsRecursosPublicados_GoogleAdsPublicacaoId",
                table: "GoogleAdsRecursosPublicados",
                column: "GoogleAdsPublicacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsRecursosPublicados_ResourceName",
                table: "GoogleAdsRecursosPublicados",
                column: "ResourceName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAdsRecursosPublicados");

            migrationBuilder.DropTable(
                name: "GoogleAdsPublicacoes");
        }
    }
}
