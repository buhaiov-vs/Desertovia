using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desertod.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProviderUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderUserName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => new { x.Provider, x.ProviderUserId });
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Villages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    Morale = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Villages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Villages_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citizens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VillageId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Stats_Strength = table.Column<int>(type: "integer", nullable: false),
                    Stats_Dexterity = table.Column<int>(type: "integer", nullable: false),
                    Stats_Intelligence = table.Column<int>(type: "integer", nullable: false),
                    Stats_Charisma = table.Column<int>(type: "integer", nullable: false),
                    Stats_Endurance = table.Column<int>(type: "integer", nullable: false),
                    Stats_StrengthExperience = table.Column<double>(type: "double precision", nullable: false),
                    Stats_DexterityExperience = table.Column<double>(type: "double precision", nullable: false),
                    Stats_IntelligenceExperience = table.Column<double>(type: "double precision", nullable: false),
                    Stats_CharismaExperience = table.Column<double>(type: "double precision", nullable: false),
                    Stats_EnduranceExperience = table.Column<double>(type: "double precision", nullable: false),
                    Renown = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StoredEnergy = table.Column<double>(type: "double precision", nullable: false),
                    EnergyUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentTaskId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CycleStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CycleEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CycleSettled = table.Column<bool>(type: "boolean", nullable: false),
                    CycleTired = table.Column<bool>(type: "boolean", nullable: false),
                    Streak = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citizens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citizens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Citizens_Villages_VillageId",
                        column: x => x.VillageId,
                        principalTable: "Villages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_AccountId",
                table: "Citizens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_CycleSettled_CycleEndsAt",
                table: "Citizens",
                columns: new[] { "CycleSettled", "CycleEndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_VillageId_AccountId",
                table: "Citizens",
                columns: new[] { "VillageId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_AccountId",
                table: "ExternalLogins",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_OwnerAccountId",
                table: "Villages",
                column: "OwnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_Slug",
                table: "Villages",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citizens");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "VillageResources");

            migrationBuilder.DropTable(
                name: "Villages");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
