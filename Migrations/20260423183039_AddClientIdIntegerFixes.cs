// Migrations/AddClientIdIntegerFixes.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    public partial class AddClientIdIntegerFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Fix 1 : Changer ClientId dans Reviews de string (longtext) à int
            // On supprime l'ancienne colonne string et on crée une nouvelle int
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Reviews");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);  // 0 = valeur par défaut pour les anciens enregistrements

            // ✅ Fix 2 : Ajouter ClientId (int) dans Bookings
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback Fix 1 : Remettre ClientId en string dans Reviews
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Reviews");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Reviews",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Rollback Fix 2 : Supprimer ClientId de Bookings
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Bookings");
        }
    }
}