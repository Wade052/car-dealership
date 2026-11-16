using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace car_dealership.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CarModelYears_CarModelId_year",
                table: "CarModelYears");

            migrationBuilder.AddColumn<int>(
                name: "VpicMakeId",
                table: "Makes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "CarModelYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VpicModelId",
                table: "CarModels",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogImportRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImportRequests", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "CarModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "VpicModelId",
                value: null);

            migrationBuilder.UpdateData(
                table: "CarModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "VpicModelId",
                value: null);

            migrationBuilder.UpdateData(
                table: "CarModels",
                keyColumn: "Id",
                keyValue: 3,
                column: "VpicModelId",
                value: null);

            migrationBuilder.UpdateData(
                table: "CarModels",
                keyColumn: "Id",
                keyValue: 4,
                column: "VpicModelId",
                value: null);

            migrationBuilder.UpdateData(
                table: "CarModels",
                keyColumn: "Id",
                keyValue: 5,
                column: "VpicModelId",
                value: null);

            migrationBuilder.UpdateData(
                table: "CarModels",
                keyColumn: "Id",
                keyValue: 6,
                column: "VpicModelId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Makes",
                keyColumn: "Id",
                keyValue: 1,
                column: "VpicMakeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Makes",
                keyColumn: "Id",
                keyValue: 2,
                column: "VpicMakeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Makes",
                keyColumn: "Id",
                keyValue: 3,
                column: "VpicMakeId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Makes_VpicMakeId",
                table: "Makes",
                column: "VpicMakeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarModelYears_CarModelId_year_VehicleType",
                table: "CarModelYears",
                columns: new[] { "CarModelId", "year", "VehicleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarModels_MakeId_VpicModelId",
                table: "CarModels",
                columns: new[] { "MakeId", "VpicModelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogImportRequests");

            migrationBuilder.DropIndex(
                name: "IX_Makes_VpicMakeId",
                table: "Makes");

            migrationBuilder.DropIndex(
                name: "IX_CarModelYears_CarModelId_year_VehicleType",
                table: "CarModelYears");

            migrationBuilder.DropIndex(
                name: "IX_CarModels_MakeId_VpicModelId",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "VpicMakeId",
                table: "Makes");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "CarModelYears");

            migrationBuilder.DropColumn(
                name: "VpicModelId",
                table: "CarModels");

            migrationBuilder.CreateIndex(
                name: "IX_CarModelYears_CarModelId_year",
                table: "CarModelYears",
                columns: new[] { "CarModelId", "year" },
                unique: true);
        }
    }
}
