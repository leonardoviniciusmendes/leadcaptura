using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaAdsVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetaAdsVideos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CampanhaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MetaAdsContaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreativeAssetId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdAccountId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeArquivo = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: true),
                    ContentHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MetaVideoId = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataUpload = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaAdsVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaAdsVideos_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetaAdsVideos_CreativeAssets_CreativeAssetId",
                        column: x => x.CreativeAssetId,
                        principalTable: "CreativeAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetaAdsVideos_MetaAdsContas_MetaAdsContaId",
                        column: x => x.MetaAdsContaId,
                        principalTable: "MetaAdsContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MetaAdsVideos_AdAccountId_ContentHash",
                table: "MetaAdsVideos",
                columns: new[] { "AdAccountId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaAdsVideos_CampanhaId",
                table: "MetaAdsVideos",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaAdsVideos_CreativeAssetId",
                table: "MetaAdsVideos",
                column: "CreativeAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaAdsVideos_MetaAdsContaId",
                table: "MetaAdsVideos",
                column: "MetaAdsContaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetaAdsVideos");
        }
    }
}
