using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MadaServices.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReviewAndProviderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<int>(
                name: "ProviderId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProviderId",
                table: "Reviews",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_ProviderId",
                table: "Reviews",
                column: "ProviderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_ProviderId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProviderId",
                table: "Reviews");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderId",
                table: "Reviews",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProviderId1",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProviderId1",
                table: "Reviews",
                column: "ProviderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_ProviderId1",
                table: "Reviews",
                column: "ProviderId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
