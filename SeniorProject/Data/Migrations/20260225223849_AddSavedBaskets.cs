using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedBaskets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedBaskets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WinningSupermarket = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBaskets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedBasketItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedBasketId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBasketItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedBasketItems_SavedBaskets_SavedBasketId",
                        column: x => x.SavedBasketId,
                        principalTable: "SavedBaskets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedBasketItems_SavedBasketId",
                table: "SavedBasketItems",
                column: "SavedBasketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedBasketItems");

            migrationBuilder.DropTable(
                name: "SavedBaskets");
        }
    }
}
