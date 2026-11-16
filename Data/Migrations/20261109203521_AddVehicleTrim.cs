using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace car_dealership.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTrim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Trim",
                table: "Cars",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Trim",
                table: "Cars");
        }
    }
}
