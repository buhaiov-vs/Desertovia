using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CitizenIdleSpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IdleSpotX",
                table: "Citizens",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IdleSpotY",
                table: "Citizens",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdleSpotX",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "IdleSpotY",
                table: "Citizens");
        }
    }
}
