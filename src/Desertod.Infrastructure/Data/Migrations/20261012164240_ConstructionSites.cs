using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConstructionSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sites",
                table: "Villages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Structures",
                table: "Villages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSiteId",
                table: "Citizens",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSiteStage",
                table: "Citizens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHungry",
                table: "Citizens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ShelterQuality",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sites",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "Structures",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "CurrentSiteId",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "CurrentSiteStage",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "IsHungry",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "ShelterQuality",
                table: "Citizens");
        }
    }
}
