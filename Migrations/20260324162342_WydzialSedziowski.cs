using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sedziowanie.Migrations
{
    /// <inheritdoc />
    public partial class WydzialSedziowski : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Komisje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Komisje", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KomisjaCzlonkowie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KomisjaId = table.Column<int>(type: "int", nullable: false),
                    SedziaId = table.Column<int>(type: "int", nullable: false),
                    Funkcja = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KomisjaCzlonkowie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KomisjaCzlonkowie_Komisje_KomisjaId",
                        column: x => x.KomisjaId,
                        principalTable: "Komisje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KomisjaCzlonkowie_Sedziowie_SedziaId",
                        column: x => x.SedziaId,
                        principalTable: "Sedziowie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KomisjaCzlonkowie_KomisjaId",
                table: "KomisjaCzlonkowie",
                column: "KomisjaId");

            migrationBuilder.CreateIndex(
                name: "IX_KomisjaCzlonkowie_SedziaId",
                table: "KomisjaCzlonkowie",
                column: "SedziaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KomisjaCzlonkowie");

            migrationBuilder.DropTable(
                name: "Komisje");
        }
    }
}
