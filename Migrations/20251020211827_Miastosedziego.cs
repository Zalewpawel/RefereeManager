using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sedziowanie.Migrations
{
    /// <inheritdoc />
    public partial class Miastosedziego : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Miasto",
                table: "Sedziowie",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Miasto",
                table: "Sedziowie");
        }
    }
}
