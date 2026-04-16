using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ImportedProducts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_Category",
                table: "ImportedProducts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_ImportDate",
                table: "ImportedProducts",
                column: "ImportDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_Category",
                table: "ImportedProducts");

            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_ImportDate",
                table: "ImportedProducts");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ImportedProducts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
