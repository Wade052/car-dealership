using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace car_dealership.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarPhotos_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarPhotos_CarId_DisplayOrder",
                table: "CarPhotos",
                columns: new[] { "CarId", "DisplayOrder" },
                unique: true);

            // Existing single photos become the first photo in each car's gallery.
            migrationBuilder.Sql("""
                INSERT INTO CarPhotos (CarId, Url, DisplayOrder)
                SELECT Id, ImageUrl, 0
                FROM Cars
                WHERE ImageUrl IS NOT NULL AND trim(ImageUrl) <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarPhotos");
        }
    }
}
