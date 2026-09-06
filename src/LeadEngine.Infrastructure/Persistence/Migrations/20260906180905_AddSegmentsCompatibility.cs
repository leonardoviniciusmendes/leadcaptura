using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentsCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignConfigJson",
                table: "Campanhas",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "SegmentId",
                table: "Campanhas",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Segments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultConfigJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Segments",
                columns: new[] { "Id", "CreatedAt", "DefaultConfigJson", "Description", "IsActive", "Name", "Slug", "TemplateKey", "UpdatedAt" },
                values: new object[] { new Guid("3f1ce0a4-7ec5-4c8f-b6d9-df4f3e7f0c35"), new DateTime(2026, 9, 6, 0, 0, 0, 0, DateTimeKind.Utc), "{\"businessCategory\":\"health_insurance_quote\",\"defaultGoal\":\"lead_generation_whatsapp\",\"legacyCompatibility\":true,\"contextDefaults\":{\"offerType\":\"quote\",\"primaryChannel\":\"whatsapp\",\"commercialApproach\":\"consultative\"},\"restrictions\":[\"nao_garantir_preco\",\"nao_garantir_cobertura\",\"nao_garantir_aprovacao\",\"nao_promover_carencia_zero\"]}", "Captação de leads para cotação consultiva de planos de saúde.", true, "Planos de Saúde", "planos-saude", "high_ticket_quote", null });

            migrationBuilder.Sql("UPDATE `Campanhas` SET `SegmentId` = '3f1ce0a4-7ec5-4c8f-b6d9-df4f3e7f0c35' WHERE `SegmentId` IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_SegmentId",
                table: "Campanhas",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Segments_IsActive",
                table: "Segments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Segments_Slug",
                table: "Segments",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Campanhas_Segments_SegmentId",
                table: "Campanhas",
                column: "SegmentId",
                principalTable: "Segments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campanhas_Segments_SegmentId",
                table: "Campanhas");

            migrationBuilder.DropTable(
                name: "Segments");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_SegmentId",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "CampaignConfigJson",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "SegmentId",
                table: "Campanhas");
        }
    }
}
