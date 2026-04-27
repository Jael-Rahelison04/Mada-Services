// Migrations/ChangeRatingToDecimal.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    public partial class ChangeRatingToDecimal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Passer Rating de int à decimal(3,1)
            // decimal(3,1) = max 3 chiffres dont 1 après la virgule → ex: 4.5
            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "Reviews",
                type: "decimal(3,1)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,1)");
        }
    }
}