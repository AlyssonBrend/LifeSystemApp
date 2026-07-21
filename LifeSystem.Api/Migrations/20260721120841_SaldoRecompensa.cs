using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class SaldoRecompensa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SaldoRecompensa",
                table: "Personagens",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SaldoRecompensa",
                table: "Personagens");
        }
    }
}
