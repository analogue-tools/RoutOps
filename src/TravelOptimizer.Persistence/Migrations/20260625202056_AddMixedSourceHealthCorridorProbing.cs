using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelOptimizer.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMixedSourceHealthCorridorProbing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorridorSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    CorridorKey = table.Column<string>(type: "text", nullable: false),
                    DayType = table.Column<string>(type: "text", nullable: false),
                    HourBucket = table.Column<string>(type: "text", nullable: false),
                    PredictedDurationMin = table.Column<int>(type: "integer", nullable: false),
                    WaitMin = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Disruptions = table.Column<string>(type: "text", nullable: false),
                    SampledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorridorSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredictionSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TravelPredictionId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    DurationMin = table.Column<int>(type: "integer", nullable: false),
                    FromLabel = table.Column<string>(type: "text", nullable: false),
                    ToLabel = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionSegments_TravelPredictions_TravelPredictionId",
                        column: x => x.TravelPredictionId,
                        principalTable: "TravelPredictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SourceHealth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    EwmaSuccessRate = table.Column<double>(type: "double precision", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    AvgMape = table.Column<double>(type: "double precision", nullable: false),
                    LastSuccessAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisabledUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceHealth", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorridorSamples_CorridorKey_Mode_SampledAt",
                table: "CorridorSamples",
                columns: new[] { "CorridorKey", "Mode", "SampledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSegments_TravelPredictionId_Order",
                table: "PredictionSegments",
                columns: new[] { "TravelPredictionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceHealth_Mode",
                table: "SourceHealth",
                column: "Mode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorridorSamples");

            migrationBuilder.DropTable(
                name: "PredictionSegments");

            migrationBuilder.DropTable(
                name: "SourceHealth");
        }
    }
}
