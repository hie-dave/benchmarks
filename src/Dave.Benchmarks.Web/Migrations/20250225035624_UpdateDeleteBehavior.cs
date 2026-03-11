using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dave.Benchmarks.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Datasets_DatasetGroups_GroupId",
                table: "Datasets");

            migrationBuilder.DropForeignKey(
                name: "FK_Individuals_Pfts_PftId",
                table: "Individuals");

            migrationBuilder.AddForeignKey(
                name: "FK_Datasets_DatasetGroups_GroupId",
                table: "Datasets",
                column: "GroupId",
                principalTable: "DatasetGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Individuals_Pfts_PftId",
                table: "Individuals",
                column: "PftId",
                principalTable: "Pfts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Datasets_DatasetGroups_GroupId",
                table: "Datasets");

            migrationBuilder.DropForeignKey(
                name: "FK_Individuals_Pfts_PftId",
                table: "Individuals");

            migrationBuilder.AddForeignKey(
                name: "FK_Datasets_DatasetGroups_GroupId",
                table: "Datasets",
                column: "GroupId",
                principalTable: "DatasetGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Individuals_Pfts_PftId",
                table: "Individuals",
                column: "PftId",
                principalTable: "Pfts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
