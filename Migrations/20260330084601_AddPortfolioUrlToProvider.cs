using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioUrlToProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PortfolioImages",
                table: "AspNetUsers",
                newName: "PortfolioUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PortfolioUrl",
                table: "AspNetUsers",
                newName: "PortfolioImages");
        }
    }
}
