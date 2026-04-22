using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sedziowanie.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rozgrywki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rozgrywki", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sedziowie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Imie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nazwisko = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Klasa = table.Column<int>(type: "int", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sedziowie", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mecze",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumerMeczu = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RozgrywkiId = table.Column<int>(type: "int", nullable: false),
                    Gospodarz = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gosc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SedziaIId = table.Column<int>(type: "int", nullable: true),
                    SedziaIIId = table.Column<int>(type: "int", nullable: true),
                    SedziaSekretarzId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mecze", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mecze_Rozgrywki_RozgrywkiId",
                        column: x => x.RozgrywkiId,
                        principalTable: "Rozgrywki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mecze_Sedziowie_SedziaIIId",
                        column: x => x.SedziaIIId,
                        principalTable: "Sedziowie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mecze_Sedziowie_SedziaIId",
                        column: x => x.SedziaIId,
                        principalTable: "Sedziowie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mecze_Sedziowie_SedziaSekretarzId",
                        column: x => x.SedziaSekretarzId,
                        principalTable: "Sedziowie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Niedyspozycje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Poczatek = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Koniec = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SedziaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Niedyspozycje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Niedyspozycje_Sedziowie_SedziaId",
                        column: x => x.SedziaId,
                        principalTable: "Sedziowie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_RozgrywkiId",
                table: "Mecze",
                column: "RozgrywkiId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_SedziaIId",
                table: "Mecze",
                column: "SedziaIId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_SedziaIIId",
                table: "Mecze",
                column: "SedziaIIId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecze_SedziaSekretarzId",
                table: "Mecze",
                column: "SedziaSekretarzId");

            migrationBuilder.CreateIndex(
                name: "IX_Niedyspozycje_SedziaId",
                table: "Niedyspozycje",
                column: "SedziaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mecze");

            migrationBuilder.DropTable(
                name: "Niedyspozycje");

            migrationBuilder.DropTable(
                name: "Rozgrywki");

            migrationBuilder.DropTable(
                name: "Sedziowie");
        }
    }
}
