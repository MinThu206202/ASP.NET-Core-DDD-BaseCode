using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCategoryPriceAndDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Categorys");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Categorys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Categorys",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Categorys",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
