using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CitizenWorkSpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SpotX",
                table: "Citizens",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpotY",
                table: "Citizens",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpotX",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "SpotY",
                table: "Citizens");
        }
    }
}
