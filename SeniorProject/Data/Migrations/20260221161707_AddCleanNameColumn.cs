using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_TownId",
                table: "ImportedProducts");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ImportedProducts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CleanName",
                table: "ImportedProducts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_CleanName",
                table: "ImportedProducts",
                column: "CleanName");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_Name",
                table: "ImportedProducts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_TownId_RetailChainId_ImportDate",
                table: "ImportedProducts",
                columns: new[] { "TownId", "RetailChainId", "ImportDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_CleanName",
                table: "ImportedProducts");

            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_Name",
                table: "ImportedProducts");

            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_TownId_RetailChainId_ImportDate",
                table: "ImportedProducts");

            migrationBuilder.DropColumn(
                name: "CleanName",
                table: "ImportedProducts");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ImportedProducts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_TownId",
                table: "ImportedProducts",
                column: "TownId");
        }
    }
}
