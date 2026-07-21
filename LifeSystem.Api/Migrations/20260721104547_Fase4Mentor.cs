using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fase4Mentor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConselhosMentor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Conteudo = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConselhosMentor", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConselhosMentor_PersonagemId_Data",
                table: "ConselhosMentor",
                columns: new[] { "PersonagemId", "Data" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConselhosMentor");
        }
    }
}
