using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sedziowanie.Migrations
{
    /// <inheritdoc />
    public partial class AddSedziowieLiniowiIGlowny20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SedziaGlownyId",
                table: "Mecze",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SedziaLiniowyIIId",
                table: "Mecze",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SedziaLiniowyIId",
                table: "Mecze",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_SedziaGlownyId",
                table: "Mecze",
                column: "SedziaGlownyId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_SedziaLiniowyIId",
                table: "Mecze",
                column: "SedziaLiniowyIId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_SedziaLiniowyIIId",
                table: "Mecze",
                column: "SedziaLiniowyIIId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mecze_Sedziowie_SedziaGlownyId",
                table: "Mecze",
                column: "SedziaGlownyId",
                principalTable: "Sedziowie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mecze_Sedziowie_SedziaLiniowyIIId",
                table: "Mecze",
                column: "SedziaLiniowyIIId",
                principalTable: "Sedziowie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mecze_Sedziowie_SedziaLiniowyIId",
                table: "Mecze",
                column: "SedziaLiniowyIId",
                principalTable: "Sedziowie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mecze_Sedziowie_SedziaGlownyId",
                table: "Mecze");

            migrationBuilder.DropForeignKey(
                name: "FK_Mecze_Sedziowie_SedziaLiniowyIIId",
                table: "Mecze");

            migrationBuilder.DropForeignKey(
                name: "FK_Mecze_Sedziowie_SedziaLiniowyIId",
                table: "Mecze");

            migrationBuilder.DropIndex(
                name: "IX_Mecze_SedziaGlownyId",
                table: "Mecze");

            migrationBuilder.DropIndex(
                name: "IX_Mecze_SedziaLiniowyIId",
                table: "Mecze");

            migrationBuilder.DropIndex(
                name: "IX_Mecze_SedziaLiniowyIIId",
                table: "Mecze");

            migrationBuilder.DropColumn(
                name: "SedziaGlownyId",
                table: "Mecze");

            migrationBuilder.DropColumn(
                name: "SedziaLiniowyIIId",
                table: "Mecze");

            migrationBuilder.DropColumn(
                name: "SedziaLiniowyIId",
                table: "Mecze");
        }
    }
}
