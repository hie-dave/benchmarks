using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dave.Benchmarks.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BenchmarkSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GitLabProjectId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MergeRequestId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PipelineId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommitSha = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommitMessage = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceBranch = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetBranch = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BenchmarkName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkSubmissions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DatasetGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsComplete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ActiveCollectionKey = table.Column<string>(type: "varchar(385)", maxLength: 385, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kind = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Source = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetGroups", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pfts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pfts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BenchmarkSubmissionId = table.Column<int>(type: "int", nullable: false),
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
                        name: "FK_EvaluationRuns_BenchmarkSubmissions_BenchmarkSubmissionId",
                        column: x => x.BenchmarkSubmissionId,
                        principalTable: "BenchmarkSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SpatialResolution = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemporalResolution = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    SimulationId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DatasetType = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatchingStrategy = table.Column<int>(type: "int", nullable: true),
                    MaxDistance = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValue: false),
                    BenchmarkSubmissionId = table.Column<int>(type: "int", nullable: true),
                    ModelVersion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClimateDataset = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PredictionDataset_Metadata = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Patches = table.Column<byte[]>(type: "longblob", nullable: true),
                    BaselineChannel = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                    table.CheckConstraint("CK_Datasets_Observation_MatchingStrategy_MaxDistance", "(DatasetType <> 'Observation') OR ((MatchingStrategy = 1 AND MaxDistance IS NOT NULL AND MaxDistance > 0) OR (MatchingStrategy <> 1 AND MaxDistance IS NULL))");
                    table.ForeignKey(
                        name: "FK_Datasets_BenchmarkSubmissions_BenchmarkSubmissionId",
                        column: x => x.BenchmarkSubmissionId,
                        principalTable: "BenchmarkSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Datasets_DatasetGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "DatasetGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvaluationRunDatasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EvaluationRunId = table.Column<int>(type: "int", nullable: false),
                    CandidateDatasetId = table.Column<int>(type: "int", nullable: false),
                    BaselineDatasetId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRunDatasets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationRunDatasets_Datasets_BaselineDatasetId",
                        column: x => x.BaselineDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationRunDatasets_Datasets_CandidateDatasetId",
                        column: x => x.CandidateDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationRunDatasets_EvaluationRuns_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "EvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Individuals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Number = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    PftId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Individuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Individuals_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Individuals_Pfts_PftId",
                        column: x => x.PftId,
                        principalTable: "Pfts",
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
                    AcceptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AcceptedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcceptedReason = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcceptedFromPipelineId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                name: "Variables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Units = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variables_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VariableLayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VariableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableLayers", x => x.Id);
                    table.UniqueConstraint("AK_VariableLayers_Id_VariableId", x => new { x.Id, x.VariableId });
                    table.ForeignKey(
                        name: "FK_VariableLayers_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Datum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<double>(type: "double", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: true),
                    Latitude = table.Column<double>(type: "double", nullable: true),
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    LayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datum", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Datum_VariableLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "VariableLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Datum_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvaluationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EvaluationRunDatasetId = table.Column<int>(type: "int", nullable: false),
                    CandidateVariableId = table.Column<int>(type: "int", nullable: false),
                    CandidateLayerId = table.Column<int>(type: "int", nullable: false),
                    BaselineVariableId = table.Column<int>(type: "int", nullable: true),
                    BaselineLayerId = table.Column<int>(type: "int", nullable: true),
                    ObservationVariableId = table.Column<int>(type: "int", nullable: false),
                    ObservationLayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationResults", x => x.Id);
                    table.CheckConstraint("CK_EvaluationResults_BaselineVariableLayerPair", "((BaselineVariableId IS NULL AND BaselineLayerId IS NULL) OR (BaselineVariableId IS NOT NULL AND BaselineLayerId IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_EvaluationResults_EvaluationRunDatasets_EvaluationRunDataset~",
                        column: x => x.EvaluationRunDatasetId,
                        principalTable: "EvaluationRunDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_VariableLayers_BaselineLayerId_BaselineVar~",
                        columns: x => new { x.BaselineLayerId, x.BaselineVariableId },
                        principalTable: "VariableLayers",
                        principalColumns: new[] { "Id", "VariableId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_VariableLayers_CandidateLayerId_CandidateV~",
                        columns: x => new { x.CandidateLayerId, x.CandidateVariableId },
                        principalTable: "VariableLayers",
                        principalColumns: new[] { "Id", "VariableId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_VariableLayers_ObservationLayerId_Observat~",
                        columns: x => new { x.ObservationLayerId, x.ObservationVariableId },
                        principalTable: "VariableLayers",
                        principalColumns: new[] { "Id", "VariableId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_Variables_BaselineVariableId",
                        column: x => x.BaselineVariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_Variables_CandidateVariableId",
                        column: x => x.CandidateVariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_Variables_ObservationVariableId",
                        column: x => x.ObservationVariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GridcellData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GridcellData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GridcellData_Datum_Id",
                        column: x => x.Id,
                        principalTable: "Datum",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvaluationMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EvaluationResultId = table.Column<int>(type: "int", nullable: false),
                    MetricType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationMetrics", x => x.Id);
                    table.CheckConstraint("CK_EvaluationMetrics_Value_IsFinite", "(Value = Value) AND (Value <= 1.7976931348623157E308) AND (Value >= -1.7976931348623157E308)");
                    table.ForeignKey(
                        name: "FK_EvaluationMetrics_EvaluationResults_EvaluationResultId",
                        column: x => x.EvaluationResultId,
                        principalTable: "EvaluationResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StandData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    StandId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandData_GridcellData_Id",
                        column: x => x.Id,
                        principalTable: "GridcellData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PatchData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PatchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatchData_StandData_Id",
                        column: x => x.Id,
                        principalTable: "StandData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IndividualData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndividualData_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "Individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndividualData_PatchData_Id",
                        column: x => x.Id,
                        principalTable: "PatchData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSubmissions_GitLabProjectId_CommitSha",
                table: "BenchmarkSubmissions",
                columns: new[] { "GitLabProjectId", "CommitSha" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSubmissions_GitLabProjectId_MergeRequestId_CreatedAt",
                table: "BenchmarkSubmissions",
                columns: new[] { "GitLabProjectId", "MergeRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSubmissions_GitLabProjectId_PipelineId_BenchmarkName",
                table: "BenchmarkSubmissions",
                columns: new[] { "GitLabProjectId", "PipelineId", "BenchmarkName" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSubmissions_Status",
                table: "BenchmarkSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetGroups_ActiveCollectionKey",
                table: "DatasetGroups",
                column: "ActiveCollectionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetGroups_Source_Name_IsActive",
                table: "DatasetGroups",
                columns: new[] { "Source", "Name", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetGroups_Source_Name_Version",
                table: "DatasetGroups",
                columns: new[] { "Source", "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Active",
                table: "Datasets",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_BenchmarkSubmissionId",
                table: "Datasets",
                column: "BenchmarkSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_GroupId",
                table: "Datasets",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_SimulationId",
                table: "Datasets",
                column: "SimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_SimulationId_BaselineChannel",
                table: "Datasets",
                columns: new[] { "SimulationId", "BaselineChannel" });

            migrationBuilder.CreateIndex(
                name: "IX_Datum_LayerId",
                table: "Datum",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Datum_Timestamp",
                table: "Datum",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Datum_VariableId",
                table: "Datum",
                column: "VariableId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationMetrics_EvaluationResultId",
                table: "EvaluationMetrics",
                column: "EvaluationResultId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationMetrics_EvaluationResultId_MetricType",
                table: "EvaluationMetrics",
                columns: new[] { "EvaluationResultId", "MetricType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_BaselineLayerId",
                table: "EvaluationResults",
                column: "BaselineLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_BaselineLayerId_BaselineVariableId",
                table: "EvaluationResults",
                columns: new[] { "BaselineLayerId", "BaselineVariableId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_BaselineVariableId",
                table: "EvaluationResults",
                column: "BaselineVariableId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_CandidateLayerId",
                table: "EvaluationResults",
                column: "CandidateLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_CandidateLayerId_CandidateVariableId",
                table: "EvaluationResults",
                columns: new[] { "CandidateLayerId", "CandidateVariableId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_CandidateVariableId",
                table: "EvaluationResults",
                column: "CandidateVariableId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EvaluationRunDatasetId",
                table: "EvaluationResults",
                column: "EvaluationRunDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EvaluationRunDatasetId_CandidateVariableId~",
                table: "EvaluationResults",
                columns: new[] { "EvaluationRunDatasetId", "CandidateVariableId", "CandidateLayerId", "ObservationVariableId", "ObservationLayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_ObservationLayerId",
                table: "EvaluationResults",
                column: "ObservationLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_ObservationLayerId_ObservationVariableId",
                table: "EvaluationResults",
                columns: new[] { "ObservationLayerId", "ObservationVariableId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_ObservationVariableId",
                table: "EvaluationResults",
                column: "ObservationVariableId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRunDatasets_BaselineDatasetId",
                table: "EvaluationRunDatasets",
                column: "BaselineDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRunDatasets_CandidateDatasetId",
                table: "EvaluationRunDatasets",
                column: "CandidateDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRunDatasets_EvaluationRunId_CandidateDatasetId",
                table: "EvaluationRunDatasets",
                columns: new[] { "EvaluationRunId", "CandidateDatasetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRunDatasets_Status",
                table: "EvaluationRunDatasets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_BenchmarkSubmissionId",
                table: "EvaluationRuns",
                column: "BenchmarkSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_Status",
                table: "EvaluationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualData_IndividualId",
                table: "IndividualData",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_Individuals_DatasetId_Number",
                table: "Individuals",
                columns: new[] { "DatasetId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Individuals_PftId",
                table: "Individuals",
                column: "PftId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchData_PatchId",
                table: "PatchData",
                column: "PatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Pfts_Name",
                table: "Pfts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionBaselineRegistryEntries_AcceptedAt",
                table: "PredictionBaselineRegistryEntries",
                column: "AcceptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionBaselineRegistryEntries_PredictionDatasetId",
                table: "PredictionBaselineRegistryEntries",
                column: "PredictionDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionBaselineRegistryEntries_SimulationId_BaselineChannel",
                table: "PredictionBaselineRegistryEntries",
                columns: new[] { "SimulationId", "BaselineChannel" });

            migrationBuilder.CreateIndex(
                name: "IX_StandData_StandId",
                table: "StandData",
                column: "StandId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableLayers_VariableId",
                table: "VariableLayers",
                column: "VariableId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_DatasetId",
                table: "Variables",
                column: "DatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationMetrics");

            migrationBuilder.DropTable(
                name: "IndividualData");

            migrationBuilder.DropTable(
                name: "PredictionBaselineRegistryEntries");

            migrationBuilder.DropTable(
                name: "EvaluationResults");

            migrationBuilder.DropTable(
                name: "Individuals");

            migrationBuilder.DropTable(
                name: "PatchData");

            migrationBuilder.DropTable(
                name: "EvaluationRunDatasets");

            migrationBuilder.DropTable(
                name: "Pfts");

            migrationBuilder.DropTable(
                name: "StandData");

            migrationBuilder.DropTable(
                name: "EvaluationRuns");

            migrationBuilder.DropTable(
                name: "GridcellData");

            migrationBuilder.DropTable(
                name: "Datum");

            migrationBuilder.DropTable(
                name: "VariableLayers");

            migrationBuilder.DropTable(
                name: "Variables");

            migrationBuilder.DropTable(
                name: "Datasets");

            migrationBuilder.DropTable(
                name: "BenchmarkSubmissions");

            migrationBuilder.DropTable(
                name: "DatasetGroups");
        }
    }
}
