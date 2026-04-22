using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sedziowanie.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsKluby : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoscKlubId",
                table: "Mecze",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GospodarzKlubId",
                table: "Mecze",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_GoscKlubId",
                table: "Mecze",
                column: "GoscKlubId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_GospodarzKlubId",
                table: "Mecze",
                column: "GospodarzKlubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mecze_AspNetUsers_GoscKlubId",
                table: "Mecze",
                column: "GoscKlubId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mecze_AspNetUsers_GospodarzKlubId",
                table: "Mecze",
                column: "GospodarzKlubId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mecze_AspNetUsers_GoscKlubId",
                table: "Mecze");

            migrationBuilder.DropForeignKey(
                name: "FK_Mecze_AspNetUsers_GospodarzKlubId",
                table: "Mecze");

            migrationBuilder.DropIndex(
                name: "IX_Mecze_GoscKlubId",
                table: "Mecze");

            migrationBuilder.DropIndex(
                name: "IX_Mecze_GospodarzKlubId",
                table: "Mecze");

            migrationBuilder.DropColumn(
                name: "GoscKlubId",
                table: "Mecze");

            migrationBuilder.DropColumn(
                name: "GospodarzKlubId",
                table: "Mecze");
        }
    }
}
