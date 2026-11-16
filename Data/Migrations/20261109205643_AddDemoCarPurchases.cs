using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace car_dealership.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoCarPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerId = table.Column<string>(type: "TEXT", nullable: true),
                    PurchasedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    DownPayment = table.Column<decimal>(type: "TEXT", nullable: false),
                    Months = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarPurchases_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CarPurchases_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarPurchases_BuyerId",
                table: "CarPurchases",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_CarPurchases_CarId",
                table: "CarPurchases",
                column: "CarId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarPurchases");
        }
    }
}
