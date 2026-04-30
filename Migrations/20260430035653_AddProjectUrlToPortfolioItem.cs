using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectUrlToPortfolioItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectUrl",
                table: "PortfolioItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectUrl",
                table: "PortfolioItems");
        }
    }
}
