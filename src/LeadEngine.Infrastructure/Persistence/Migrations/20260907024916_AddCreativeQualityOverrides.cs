using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreativeQualityOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreativeQualityOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CampaignId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreativeAssetId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreativeAssetAnalysisId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RankingScore = table.Column<int>(type: "int", nullable: true),
                    SemanticMismatch = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    User = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreativeQualityOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreativeQualityOverrides_Campanhas_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreativeQualityOverrides_CreativeAssetAnalyses_CreativeAsset~",
                        column: x => x.CreativeAssetAnalysisId,
                        principalTable: "CreativeAssetAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreativeQualityOverrides_CreativeAssets_CreativeAssetId",
                        column: x => x.CreativeAssetId,
                        principalTable: "CreativeAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CreativeQualityOverrides_CampaignId",
                table: "CreativeQualityOverrides",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CreativeQualityOverrides_CreatedAt",
                table: "CreativeQualityOverrides",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreativeQualityOverrides_CreativeAssetAnalysisId",
                table: "CreativeQualityOverrides",
                column: "CreativeAssetAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_CreativeQualityOverrides_CreativeAssetId",
                table: "CreativeQualityOverrides",
                column: "CreativeAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
