using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dave.Benchmarks.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDatasetTypeDiscriminator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Data");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Variables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClimateDataset",
                table: "Datasets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DatasetType",
                table: "Datasets",
                type: "varchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InputDataSource",
                table: "Datasets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "Datasets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: "Parameters",
                table: "Datasets",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Patches",
                table: "Datasets",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Datasets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Datasets",
                type: "longtext",
                nullable: true)
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
                    table.ForeignKey(
                        name: "FK_VariableLayers_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GridcellData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Value = table.Column<double>(type: "double", nullable: false),
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    LayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GridcellData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GridcellData_VariableLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "VariableLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GridcellData_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PatchData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StandId = table.Column<int>(type: "int", nullable: false),
                    PatchId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Value = table.Column<double>(type: "double", nullable: false),
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    LayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatchData_VariableLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "VariableLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatchData_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StandData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StandId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Value = table.Column<double>(type: "double", nullable: false),
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    LayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandData_VariableLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "VariableLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandData_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GridcellData_LayerId",
                table: "GridcellData",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GridcellData_VariableId_LayerId_Timestamp",
                table: "GridcellData",
                columns: new[] { "VariableId", "LayerId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PatchData_LayerId",
                table: "PatchData",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchData_VariableId_LayerId_StandId_PatchId_Timestamp",
                table: "PatchData",
                columns: new[] { "VariableId", "LayerId", "StandId", "PatchId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_StandData_LayerId",
                table: "StandData",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_StandData_VariableId_LayerId_StandId_Timestamp",
                table: "StandData",
                columns: new[] { "VariableId", "LayerId", "StandId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableLayers_VariableId",
                table: "VariableLayers",
                column: "VariableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GridcellData");

            migrationBuilder.DropTable(
                name: "PatchData");

            migrationBuilder.DropTable(
                name: "StandData");

            migrationBuilder.DropTable(
                name: "VariableLayers");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "ClimateDataset",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "DatasetType",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "InputDataSource",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "Parameters",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "Patches",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Datasets");

            migrationBuilder.CreateTable(
                name: "Data",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Value = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Datasets_Id",
                        column: x => x.Id,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ClimateDataset = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodePatches = table.Column<byte[]>(type: "longblob", nullable: false),
                    CompressedParameters = table.Column<byte[]>(type: "longblob", nullable: false),
                    InputDataSource = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModelVersion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_Datasets_Id",
                        column: x => x.Id,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Data_DatasetId_VariableId_Longitude_Latitude",
                table: "Data",
                columns: new[] { "DatasetId", "VariableId", "Longitude", "Latitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Data_DatasetId_VariableId_Timestamp",
                table: "Data",
                columns: new[] { "DatasetId", "VariableId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Data_VariableId",
                table: "Data",
                column: "VariableId");
        }
    }
}
