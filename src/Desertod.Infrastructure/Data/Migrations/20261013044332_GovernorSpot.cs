using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GovernorSpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GovernorSpotX",
                table: "Villages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GovernorSpotY",
                table: "Villages",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GovernorSpotX",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "GovernorSpotY",
                table: "Villages");
        }
    }
}
