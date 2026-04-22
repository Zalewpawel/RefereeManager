using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sedziowanie.Migrations
{
    /// <inheritdoc />
    public partial class change : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Sedziowie_SedziaId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SedziaId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SedziaId",
                table: "AspNetUsers",
                column: "SedziaId",
                unique: true,
                filter: "[SedziaId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Sedziowie_SedziaId",
                table: "AspNetUsers",
                column: "SedziaId",
                principalTable: "Sedziowie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Sedziowie_SedziaId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SedziaId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SedziaId",
                table: "AspNetUsers",
                column: "SedziaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Sedziowie_SedziaId",
                table: "AspNetUsers",
                column: "SedziaId",
                principalTable: "Sedziowie",
                principalColumn: "Id");
        }
    }
}
