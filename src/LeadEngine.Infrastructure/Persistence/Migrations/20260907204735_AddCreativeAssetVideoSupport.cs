using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreativeAssetVideoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "CreativeAssets",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "CreativeAssets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "CreativeAssets",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
