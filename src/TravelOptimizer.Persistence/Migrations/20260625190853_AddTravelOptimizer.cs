using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelOptimizer.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelOptimizer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorridorModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    CorridorKey = table.Column<string>(type: "text", nullable: false),
                    DayType = table.Column<string>(type: "text", nullable: false),
                    HourBucket = table.Column<string>(type: "text", nullable: false),
                    CorrectionFactor = table.Column<double>(type: "double precision", nullable: false),
                    Mape = table.Column<double>(type: "double precision", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorridorModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyWeights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProposedAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    Change = table.Column<string>(type: "text", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    ShadowImprovementMin = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Lng = table.Column<double>(type: "double precision", nullable: false),
                    HasCoordinates = table.Column<bool>(type: "boolean", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleCalendarConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    CalendarId = table.Column<string>(type: "text", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleCalendarConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleCalendarConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelLegs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FromLabel = table.Column<string>(type: "text", nullable: false),
                    FromLat = table.Column<double>(type: "double precision", nullable: false),
                    FromLng = table.Column<double>(type: "double precision", nullable: false),
                    ToLabel = table.Column<string>(type: "text", nullable: false),
                    ToLat = table.Column<double>(type: "double precision", nullable: false),
                    ToLng = table.Column<double>(type: "double precision", nullable: false),
                    ArriveBy = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorridorKey = table.Column<string>(type: "text", nullable: false),
                    DayType = table.Column<string>(type: "text", nullable: false),
                    HourBucket = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelLegs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TravelLegId = table.Column<int>(type: "integer", nullable: false),
                    ChosenMode = table.Column<string>(type: "text", nullable: false),
                    RecommendedDeparture = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedWastedMin = table.Column<int>(type: "integer", nullable: false),
                    WasExploration = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelDecisions_TravelLegs_TravelLegId",
                        column: x => x.TravelLegId,
                        principalTable: "TravelLegs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelPredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TravelLegId = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    RawDurationMin = table.Column<int>(type: "integer", nullable: false),
                    CalibratedDurationMin = table.Column<int>(type: "integer", nullable: false),
                    WaitMin = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Feasible = table.Column<bool>(type: "boolean", nullable: false),
                    Disruptions = table.Column<string>(type: "text", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPredictions_TravelLegs_TravelLegId",
                        column: x => x.TravelLegId,
                        principalTable: "TravelLegs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegOutcomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TravelDecisionId = table.Column<int>(type: "integer", nullable: false),
                    ActualArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualDurationMin = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ArrivedOnTime = table.Column<bool>(type: "boolean", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegOutcomes_TravelDecisions_TravelDecisionId",
                        column: x => x.TravelDecisionId,
                        principalTable: "TravelDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_UserId_ExternalId",
                table: "CalendarEvents",
                columns: new[] { "UserId", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_UserId_StartUtc",
                table: "CalendarEvents",
                columns: new[] { "UserId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CorridorModels_Mode_CorridorKey_DayType_HourBucket",
                table: "CorridorModels",
                columns: new[] { "Mode", "CorridorKey", "DayType", "HourBucket" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleCalendarConnections_UserId",
                table: "GoogleCalendarConnections",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegOutcomes_TravelDecisionId",
                table: "LegOutcomes",
                column: "TravelDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyWeights_UserId_Key_IsActive",
                table: "PolicyWeights",
                columns: new[] { "UserId", "Key", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposedAdjustments_UserId_Status",
                table: "ProposedAdjustments",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelDecisions_TravelLegId",
                table: "TravelDecisions",
                column: "TravelLegId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelLegs_UserId_CreatedAt",
                table: "TravelLegs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelPredictions_TravelLegId",
                table: "TravelPredictions",
                column: "TravelLegId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "CorridorModels");

            migrationBuilder.DropTable(
                name: "GoogleCalendarConnections");

            migrationBuilder.DropTable(
                name: "LegOutcomes");

            migrationBuilder.DropTable(
                name: "PolicyWeights");

            migrationBuilder.DropTable(
                name: "ProposedAdjustments");

            migrationBuilder.DropTable(
                name: "TravelPredictions");

            migrationBuilder.DropTable(
                name: "TravelDecisions");

            migrationBuilder.DropTable(
                name: "TravelLegs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
