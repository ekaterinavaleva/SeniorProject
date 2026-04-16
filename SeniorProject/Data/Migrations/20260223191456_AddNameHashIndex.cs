using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNameHashIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NameHash",
                table: "ImportedProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedProducts_NameHash",
                table: "ImportedProducts",
                column: "NameHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportedProducts_NameHash",
                table: "ImportedProducts");

            migrationBuilder.DropColumn(
                name: "NameHash",
                table: "ImportedProducts");
        }
    }
}
