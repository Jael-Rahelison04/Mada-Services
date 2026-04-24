// Migrations/FixProviderImageAndAddVerificationDoc.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    public partial class FixProviderImageAndAddVerificationDoc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Fix P11 : Supprimer ProfileImageUrl (doublon de ImageUrl dans User)
            // On vérifie d'abord si la colonne existe pour éviter une erreur
            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "AspNetUsers");

            // ✅ Fix P10 : Ajouter la colonne pour stocker le document de vérification
            migrationBuilder.AddColumn<string>(
                name: "VerificationDocumentPath",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback : Remettre ProfileImageUrl
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Rollback : Supprimer VerificationDocumentPath
            migrationBuilder.DropColumn(
                name: "VerificationDocumentPath",
                table: "AspNetUsers");
        }
    }
}