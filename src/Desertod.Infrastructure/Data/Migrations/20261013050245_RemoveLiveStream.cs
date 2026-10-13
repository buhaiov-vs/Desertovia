using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLiveStream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Villages_IsPrivate_IsLive",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "Villages");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_IsPrivate",
                table: "Villages",
                column: "IsPrivate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Villages_IsPrivate",
                table: "Villages");

            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "Villages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Villages_IsPrivate_IsLive",
                table: "Villages",
                columns: new[] { "IsPrivate", "IsLive" });
        }
    }
}
