using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class StorageBuildings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VillageResources");

            migrationBuilder.AddColumn<double>(
                name: "Knowledge",
                table: "Villages",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Storages",
                table: "Villages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCarrying",
                table: "Citizens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Load",
                table: "Citizens",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_IsCarrying",
                table: "Citizens",
                column: "IsCarrying");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Citizens_IsCarrying",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Knowledge",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "Storages",
                table: "Villages");

            migrationBuilder.DropColumn(
                name: "IsCarrying",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Load",
                table: "Citizens");

            migrationBuilder.CreateTable(
                name: "VillageResources",
                columns: table => new
                {
                    VillageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Capacity = table.Column<double>(type: "double precision", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VillageResources", x => new { x.VillageId, x.Kind });
                    table.ForeignKey(
                        name: "FK_VillageResources_Villages_VillageId",
                        column: x => x.VillageId,
                        principalTable: "Villages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
