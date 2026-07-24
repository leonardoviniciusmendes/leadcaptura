using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignGenerationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeneficiosJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataGeracao",
                table: "Campanhas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricoesAnunciosJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "DuracaoGeracaoMs",
                table: "Campanhas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErroGeracao",
                table: "Campanhas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ModeloIa",
                table: "Campanhas",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PalavrasChaveJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PalavrasChaveNegativasJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PerguntasFrequentesJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProviderIa",
                table: "Campanhas",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TitulosAnunciosJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeneficiosJson",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "DataGeracao",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "DescricoesAnunciosJson",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "DuracaoGeracaoMs",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "ErroGeracao",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "ModeloIa",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "PalavrasChaveJson",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "PalavrasChaveNegativasJson",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "PerguntasFrequentesJson",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "ProviderIa",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "TitulosAnunciosJson",
                table: "Campanhas");
        }
    }
}
