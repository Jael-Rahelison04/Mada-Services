using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProviderId1",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProviderId1",
                table: "Reviews",
                column: "ProviderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_ProviderId1",
                table: "Reviews",
                column: "ProviderId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_ProviderId1",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProviderId1",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProviderId1",
                table: "Reviews");
        }
    }
}
