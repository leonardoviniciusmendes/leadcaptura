using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreativeAssetSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CreativeAssets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CreativeAssets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CreativeAssets_IsDeleted",
                table: "CreativeAssets",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreativeAssets_IsDeleted",
                table: "CreativeAssets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CreativeAssets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CreativeAssets");
        }
    }
}
