using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace car_dealership.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDealershipLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Location",
                table: "Cars",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Cars");
        }
    }
}
