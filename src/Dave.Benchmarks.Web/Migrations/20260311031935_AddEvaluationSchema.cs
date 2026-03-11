using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dave.Benchmarks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SimulationId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaselineChannel = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CandidatePredictionDatasetId = table.Column<int>(type: "int", nullable: false),
                    BaselinePredictionDatasetId = table.Column<int>(type: "int", nullable: true),
                    ObservationBaselineDatasetId = table.Column<int>(type: "int", nullable: true),
                    MergeRequestId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceBranch = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetBranch = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommitSha = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationRuns_Datasets_BaselinePredictionDatasetId",
                        column: x => x.BaselinePredictionDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationRuns_Datasets_CandidatePredictionDatasetId",
                        column: x => x.CandidatePredictionDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationRuns_Datasets_ObservationBaselineDatasetId",
                        column: x => x.ObservationBaselineDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ObservationBaselineRegistryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SimulationId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaselineChannel = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ObservationDatasetId = table.Column<int>(type: "int", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservationBaselineRegistryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObservationBaselineRegistryEntries_Datasets_ObservationDatas~",
                        column: x => x.ObservationDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PredictionBaselineRegistryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SimulationId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaselineChannel = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PredictionDatasetId = table.Column<int>(type: "int", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionBaselineRegistryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionBaselineRegistryEntries_Datasets_PredictionDataset~",
                        column: x => x.PredictionDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvaluationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EvaluationRunId = table.Column<int>(type: "int", nullable: false),
                    VariableName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LayerName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatchedPointCount = table.Column<int>(type: "int", nullable: false),
                    NumericMismatchCount = table.Column<int>(type: "int", nullable: false),
                    StructuralMismatch = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    R2 = table.Column<double>(type: "double", nullable: true),
                    Rsr = table.Column<double>(type: "double", nullable: true),
                    Nse = table.Column<double>(type: "double", nullable: true),
                    N = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_EvaluationRuns_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "EvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EvaluationRunId",
                table: "EvaluationResults",
                column: "EvaluationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EvaluationRunId_VariableName_LayerName",
                table: "EvaluationResults",
                columns: new[] { "EvaluationRunId", "VariableName", "LayerName" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_BaselinePredictionDatasetId",
                table: "EvaluationRuns",
                column: "BaselinePredictionDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_CandidatePredictionDatasetId",
                table: "EvaluationRuns",
                column: "CandidatePredictionDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_ObservationBaselineDatasetId",
                table: "EvaluationRuns",
                column: "ObservationBaselineDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_SimulationId_BaselineChannel",
                table: "EvaluationRuns",
                columns: new[] { "SimulationId", "BaselineChannel" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_Status",
                table: "EvaluationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ObservationBaselineRegistryEntries_ObservationDatasetId",
                table: "ObservationBaselineRegistryEntries",
                column: "ObservationDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_ObservationBaselineRegistryEntries_SimulationId_BaselineChan~",
                table: "ObservationBaselineRegistryEntries",
                columns: new[] { "SimulationId", "BaselineChannel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionBaselineRegistryEntries_PredictionDatasetId",
                table: "PredictionBaselineRegistryEntries",
                column: "PredictionDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionBaselineRegistryEntries_SimulationId_BaselineChann~",
                table: "PredictionBaselineRegistryEntries",
                columns: new[] { "SimulationId", "BaselineChannel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationResults");

            migrationBuilder.DropTable(
                name: "ObservationBaselineRegistryEntries");

            migrationBuilder.DropTable(
                name: "PredictionBaselineRegistryEntries");

            migrationBuilder.DropTable(
                name: "EvaluationRuns");
        }
    }
}
