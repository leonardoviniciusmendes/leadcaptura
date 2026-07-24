using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAdsTypedPublishingAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTestAccount",
                table: "GoogleAdsPublicacoes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GoogleAdsOperacoesPublicacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Indice = table.Column<int>(type: "int", nullable: false),
                    TipoOperacao = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntidadeOrigem = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceNameTemporario = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceNameDefinitivo = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoErro = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MensagemControlada = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataConclusao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsOperacoesPublicacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsOperacoesPublicacao_GoogleAdsPublicacoes_GoogleAdsP~",
                        column: x => x.GoogleAdsPublicacaoId,
                        principalTable: "GoogleAdsPublicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GoogleAdsPublicacaoHistoricos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleAdsPublicacaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StatusAnterior = table.Column<int>(type: "int", nullable: true),
                    StatusNovo = table.Column<int>(type: "int", nullable: false),
                    Operacao = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MensagemControlada = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MetadadosJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsPublicacaoHistoricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsPublicacaoHistoricos_GoogleAdsPublicacoes_GoogleAds~",
                        column: x => x.GoogleAdsPublicacaoId,
                        principalTable: "GoogleAdsPublicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsOperacoesPublicacao_GoogleAdsPublicacaoId",
                table: "GoogleAdsOperacoesPublicacao",
                column: "GoogleAdsPublicacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsOperacoesPublicacao_GoogleAdsPublicacaoId_Indice",
                table: "GoogleAdsOperacoesPublicacao",
                columns: new[] { "GoogleAdsPublicacaoId", "Indice" });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsPublicacaoHistoricos_Data",
                table: "GoogleAdsPublicacaoHistoricos",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsPublicacaoHistoricos_GoogleAdsPublicacaoId",
                table: "GoogleAdsPublicacaoHistoricos",
                column: "GoogleAdsPublicacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAdsOperacoesPublicacao");

            migrationBuilder.DropTable(
                name: "GoogleAdsPublicacaoHistoricos");

            migrationBuilder.DropColumn(
                name: "IsTestAccount",
                table: "GoogleAdsPublicacoes");
        }
    }
}
