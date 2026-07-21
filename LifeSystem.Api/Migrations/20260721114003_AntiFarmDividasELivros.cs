using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AntiFarmDividasELivros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Premiado",
                table: "Livros",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Premiada",
                table: "Dividas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Premiado",
                table: "Livros");

            migrationBuilder.DropColumn(
                name: "Premiada",
                table: "Dividas");
        }
    }
}
