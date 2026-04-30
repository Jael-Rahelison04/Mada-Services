using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VerificationDocumentPath",
                table: "AspNetUsers",
                newName: "ResidenceCertPath");

            migrationBuilder.AddColumn<string>(
                name: "CinDocumentPath",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CvDocumentPath",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DiplomaDocumentPath",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CinDocumentPath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CvDocumentPath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DiplomaDocumentPath",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "ResidenceCertPath",
                table: "AspNetUsers",
                newName: "VerificationDocumentPath");
        }
    }
}
