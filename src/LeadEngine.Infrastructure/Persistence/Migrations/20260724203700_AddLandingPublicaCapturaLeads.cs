using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLandingPublicaCapturaLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampanhaId",
                table: "Leads",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Fbclid",
                table: "Leads",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Gclid",
                table: "Leads",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IpHash",
                table: "Leads",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Observacao",
                table: "Leads",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OrigemCaptura",
                table: "Leads",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StatusEnvioExterno",
                table: "Leads",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TentativasEnvioExterno",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoContratacao",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoErroEnvioExterno",
                table: "Leads",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserAgentResumo",
                table: "Leads",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "Leads",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UtmContent",
                table: "Leads",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "Leads",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "Leads",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "Leads",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Campanhas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataDespublicacao",
                table: "Campanhas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPublicacao",
                table: "Campanhas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Publicada",
                table: "Campanhas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UrlPublica",
                table: "Campanhas",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CampanhaId",
                table: "Leads",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CampanhaId_WhatsAppNormalizado_CriadoEm",
                table: "Leads",
                columns: new[] { "CampanhaId", "WhatsAppNormalizado", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Publicada_Ativo",
                table: "Campanhas",
                columns: new[] { "Publicada", "Ativo" });

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Campanhas_CampanhaId",
                table: "Leads",
                column: "CampanhaId",
                principalTable: "Campanhas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Campanhas_CampanhaId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_CampanhaId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_CampanhaId_WhatsAppNormalizado_CriadoEm",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_Publicada_Ativo",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "CampanhaId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Fbclid",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Gclid",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "IpHash",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Observacao",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "OrigemCaptura",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "StatusEnvioExterno",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "TentativasEnvioExterno",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "TipoContratacao",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UltimoErroEnvioExterno",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UserAgentResumo",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UtmContent",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "DataDespublicacao",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "DataPublicacao",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "Publicada",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "UrlPublica",
                table: "Campanhas");
        }
    }
}
