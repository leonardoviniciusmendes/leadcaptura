using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicLeadForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LeadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FieldKey = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LabelSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAnswers_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeadForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CampaignId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SubmitButtonText = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadForms_Campanhas_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeadFormFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LeadFormId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Placeholder = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OptionsJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidationJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    DefaultValue = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadFormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadFormFields_LeadForms_LeadFormId",
                        column: x => x.LeadFormId,
                        principalTable: "LeadForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Segments",
                keyColumn: "Id",
                keyValue: new Guid("3f1ce0a4-7ec5-4c8f-b6d9-df4f3e7f0c35"),
                column: "DefaultConfigJson",
                value: "{\"businessCategory\":\"health_insurance_quote\",\"defaultGoal\":\"lead_generation_whatsapp\",\"legacyCompatibility\":true,\"contextDefaults\":{\"offerType\":\"quote\",\"primaryChannel\":\"whatsapp\",\"commercialApproach\":\"consultative\"},\"restrictions\":[\"nao_garantir_preco\",\"nao_garantir_cobertura\",\"nao_garantir_aprovacao\",\"nao_promover_carencia_zero\"],\"leadForm\":{\"submitButtonText\":\"Receber cotacao\",\"fields\":[{\"key\":\"name\",\"label\":\"Nome\",\"type\":\"text\",\"required\":true,\"order\":1},{\"key\":\"phone\",\"label\":\"WhatsApp\",\"type\":\"phone\",\"required\":true,\"placeholder\":\"(00) 00000-0000\",\"order\":2},{\"key\":\"quantidadeVidas\",\"label\":\"Quantidade de vidas\",\"type\":\"number\",\"required\":true,\"validation\":{\"min\":1,\"max\":999},\"order\":3}]}}");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAnswers_LeadId",
                table: "LeadAnswers",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAnswers_LeadId_FieldKey",
                table: "LeadAnswers",
                columns: new[] { "LeadId", "FieldKey" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadFormFields_LeadFormId",
                table: "LeadFormFields",
                column: "LeadFormId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadFormFields_LeadFormId_Key",
                table: "LeadFormFields",
                columns: new[] { "LeadFormId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeadFormFields_LeadFormId_Order",
                table: "LeadFormFields",
                columns: new[] { "LeadFormId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadForms_CampaignId",
                table: "LeadForms",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadForms_CampaignId_IsActive",
                table: "LeadForms",
                columns: new[] { "CampaignId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadForms_CampaignId_Version",
                table: "LeadForms",
                columns: new[] { "CampaignId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadAnswers");

            migrationBuilder.DropTable(
                name: "LeadFormFields");

            migrationBuilder.DropTable(
                name: "LeadForms");

            migrationBuilder.UpdateData(
                table: "Segments",
                keyColumn: "Id",
                keyValue: new Guid("3f1ce0a4-7ec5-4c8f-b6d9-df4f3e7f0c35"),
                column: "DefaultConfigJson",
                value: "{\"businessCategory\":\"health_insurance_quote\",\"defaultGoal\":\"lead_generation_whatsapp\",\"legacyCompatibility\":true,\"contextDefaults\":{\"offerType\":\"quote\",\"primaryChannel\":\"whatsapp\",\"commercialApproach\":\"consultative\"},\"restrictions\":[\"nao_garantir_preco\",\"nao_garantir_cobertura\",\"nao_garantir_aprovacao\",\"nao_promover_carencia_zero\"]}");
        }
    }
}
