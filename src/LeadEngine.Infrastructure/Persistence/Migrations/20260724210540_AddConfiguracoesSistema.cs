using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracoesSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesSistema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Chave = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorProtegido = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sensivel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSistema", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConfiguracoesSistemaHistorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConfiguracaoSistemaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Chave = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    ValorAnterior = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorNovo = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sensivel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OrigemAlteracao = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSistemaHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesSistemaHistorico_ConfiguracoesSistema_Configura~",
                        column: x => x.ConfiguracaoSistemaId,
                        principalTable: "ConfiguracoesSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistema_Categoria",
                table: "ConfiguracoesSistema",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistema_Chave",
                table: "ConfiguracoesSistema",
                column: "Chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistemaHistorico_Categoria",
                table: "ConfiguracoesSistemaHistorico",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistemaHistorico_Chave",
                table: "ConfiguracoesSistemaHistorico",
                column: "Chave");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistemaHistorico_ConfiguracaoSistemaId",
                table: "ConfiguracoesSistemaHistorico",
                column: "ConfiguracaoSistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistemaHistorico_DataAlteracao",
                table: "ConfiguracoesSistemaHistorico",
                column: "DataAlteracao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesSistemaHistorico");

            migrationBuilder.DropTable(
                name: "ConfiguracoesSistema");
        }
    }
}
