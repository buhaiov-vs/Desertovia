using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Technologies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Research",
                table: "Villages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentTechnologyId",
                table: "Citizens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnologyVote",
                table: "Citizens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Research",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "CurrentTechnologyId",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "TechnologyVote",
                table: "Citizens");
        }
    }
}
