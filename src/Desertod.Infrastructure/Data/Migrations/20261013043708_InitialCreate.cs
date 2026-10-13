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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Character = table.Column<string>(type: "jsonb", nullable: true)
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
                name: "PasswordLogins",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RegisteredFrom = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordLogins", x => x.Email);
                    table.ForeignKey(
                        name: "FK_PasswordLogins_Accounts_AccountId",
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
                    Description = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    InviteCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    Morale = table.Column<double>(type: "double precision", nullable: false),
                    MoraleChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Knowledge = table.Column<int>(type: "integer", nullable: false),
                    GoldVeinMined = table.Column<int>(type: "integer", nullable: false),
                    ChosenTechnology = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Fields = table.Column<string>(type: "jsonb", nullable: true),
                    Forecasts = table.Column<string>(type: "jsonb", nullable: true),
                    Poll = table.Column<string>(type: "jsonb", nullable: true),
                    Research = table.Column<string>(type: "jsonb", nullable: true),
                    Sites = table.Column<string>(type: "jsonb", nullable: true),
                    Storages = table.Column<string>(type: "jsonb", nullable: true),
                    Structures = table.Column<string>(type: "jsonb", nullable: true),
                    Trees = table.Column<string>(type: "jsonb", nullable: true),
                    Wildlife = table.Column<string>(type: "jsonb", nullable: true)
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
                    Subtitle = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Stats_Strength = table.Column<int>(type: "integer", nullable: false),
                    Stats_Dexterity = table.Column<int>(type: "integer", nullable: false),
                    Stats_Intelligence = table.Column<int>(type: "integer", nullable: false),
                    Stats_Charisma = table.Column<int>(type: "integer", nullable: false),
                    Stats_Endurance = table.Column<int>(type: "integer", nullable: false),
                    Stats_StrengthExperience = table.Column<int>(type: "integer", nullable: false),
                    Stats_DexterityExperience = table.Column<int>(type: "integer", nullable: false),
                    Stats_IntelligenceExperience = table.Column<int>(type: "integer", nullable: false),
                    Stats_CharismaExperience = table.Column<int>(type: "integer", nullable: false),
                    Stats_EnduranceExperience = table.Column<int>(type: "integer", nullable: false),
                    Renown = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StoredEnergy = table.Column<double>(type: "double precision", nullable: false),
                    EnergyUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastDrinkAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentTaskId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CycleStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CycleEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CycleSettled = table.Column<bool>(type: "boolean", nullable: false),
                    CycleTired = table.Column<bool>(type: "boolean", nullable: false),
                    Streak = table.Column<int>(type: "integer", nullable: false),
                    CurrentSiteId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    CurrentSiteStage = table.Column<int>(type: "integer", nullable: true),
                    SpotX = table.Column<double>(type: "double precision", nullable: true),
                    SpotY = table.Column<double>(type: "double precision", nullable: true),
                    IdleSpotX = table.Column<double>(type: "double precision", nullable: true),
                    IdleSpotY = table.Column<double>(type: "double precision", nullable: true),
                    CurrentTechnologyId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ShelterQuality = table.Column<double>(type: "double precision", nullable: false),
                    IsHungry = table.Column<bool>(type: "boolean", nullable: false),
                    CyclesSinceMeal = table.Column<int>(type: "integer", nullable: false),
                    IsCarrying = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Appearance = table.Column<string>(type: "jsonb", nullable: true),
                    Load = table.Column<string>(type: "jsonb", nullable: true),
                    Skills = table.Column<string>(type: "jsonb", nullable: true)
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
                name: "VillageProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VillageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SurveyProgress = table.Column<int>(type: "integer", nullable: false),
                    BuildProgress = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VillageProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VillageProjects_Villages_VillageId",
                        column: x => x.VillageId,
                        principalTable: "Villages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VillageQuests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VillageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Resource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Key = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VillageQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VillageQuests_Villages_VillageId",
                        column: x => x.VillageId,
                        principalTable: "Villages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectContribution",
                columns: table => new
                {
                    VillageProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectContribution", x => new { x.VillageProjectId, x.CitizenId, x.Key });
                    table.ForeignKey(
                        name: "FK_ProjectContribution_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectContribution_VillageProjects_VillageProjectId",
                        column: x => x.VillageProjectId,
                        principalTable: "VillageProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMaterial",
                columns: table => new
                {
                    VillageProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Delivered = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMaterial", x => new { x.VillageProjectId, x.Kind });
                    table.ForeignKey(
                        name: "FK_ProjectMaterial_VillageProjects_VillageProjectId",
                        column: x => x.VillageProjectId,
                        principalTable: "VillageProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestContribution",
                columns: table => new
                {
                    VillageQuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestContribution", x => new { x.VillageQuestId, x.CitizenId });
                    table.ForeignKey(
                        name: "FK_QuestContribution_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestContribution_VillageQuests_VillageQuestId",
                        column: x => x.VillageQuestId,
                        principalTable: "VillageQuests",
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
                name: "IX_Citizens_IsCarrying",
                table: "Citizens",
                column: "IsCarrying");

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
                name: "IX_PasswordLogins_AccountId",
                table: "PasswordLogins",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordLogins_RegisteredFrom_RegisteredAt",
                table: "PasswordLogins",
                columns: new[] { "RegisteredFrom", "RegisteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContribution_CitizenId",
                table: "ProjectContribution",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestContribution_CitizenId",
                table: "QuestContribution",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_VillageProjects_VillageId_Status",
                table: "VillageProjects",
                columns: new[] { "VillageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VillageQuests_VillageId_Key",
                table: "VillageQuests",
                columns: new[] { "VillageId", "Key" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_IsPrivate_IsLive",
                table: "Villages",
                columns: new[] { "IsPrivate", "IsLive" });

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
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "PasswordLogins");

            migrationBuilder.DropTable(
                name: "ProjectContribution");

            migrationBuilder.DropTable(
                name: "ProjectMaterial");

            migrationBuilder.DropTable(
                name: "QuestContribution");

            migrationBuilder.DropTable(
                name: "VillageProjects");

            migrationBuilder.DropTable(
                name: "Citizens");

            migrationBuilder.DropTable(
                name: "VillageQuests");

            migrationBuilder.DropTable(
                name: "Villages");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
