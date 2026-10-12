using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VillagePrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Villages",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Villages",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Villages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Villages_IsPrivate_IsLive",
                table: "Villages",
                columns: new[] { "IsPrivate", "IsLive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Villages_IsPrivate_IsLive",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Villages");
        }
    }
}
