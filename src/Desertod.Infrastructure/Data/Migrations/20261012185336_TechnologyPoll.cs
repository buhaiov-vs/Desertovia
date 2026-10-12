using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TechnologyPoll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TechnologyVote",
                table: "Citizens");

            migrationBuilder.AddColumn<string>(
                name: "ChosenTechnology",
                table: "Villages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Poll",
                table: "Villages",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChosenTechnology",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "Poll",
                table: "Villages");

            migrationBuilder.AddColumn<string>(
                name: "TechnologyVote",
                table: "Citizens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
