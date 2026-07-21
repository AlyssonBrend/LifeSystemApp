using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fase3MenteBolso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HabilidadeId",
                table: "SessoesFoco",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvisoFinancasAceitoEm",
                table: "Personagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Aportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aportes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dividas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    ValorAtual = table.Column<decimal>(type: "TEXT", nullable: false),
                    JurosPctMes = table.Column<double>(type: "REAL", nullable: false),
                    QuitadaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dividas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Habilidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrilhaId = table.Column<string>(type: "TEXT", nullable: true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habilidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InteracoesSociais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteracoesSociais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Livros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    HabilidadeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ConcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Livros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfisFinanceiros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    RendaMensal = table.Column<decimal>(type: "TEXT", nullable: false),
                    DespesasFixas = table.Column<decimal>(type: "TEXT", nullable: false),
                    DespesasVariaveis = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrcamentoRecompensa = table.Column<decimal>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisFinanceiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfisFinanceiros_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarcosConcluidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HabilidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    MarcoIndice = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcosConcluidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarcosConcluidos_Habilidades_HabilidadeId",
                        column: x => x.HabilidadeId,
                        principalTable: "Habilidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aportes_PersonagemId_Data",
                table: "Aportes",
                columns: new[] { "PersonagemId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Dividas_PersonagemId",
                table: "Dividas",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Habilidades_PersonagemId",
                table: "Habilidades",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_InteracoesSociais_PersonagemId_Data",
                table: "InteracoesSociais",
                columns: new[] { "PersonagemId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Livros_PersonagemId",
                table: "Livros",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarcosConcluidos_HabilidadeId_MarcoIndice",
                table: "MarcosConcluidos",
                columns: new[] { "HabilidadeId", "MarcoIndice" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfisFinanceiros_PersonagemId",
                table: "PerfisFinanceiros",
                column: "PersonagemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aportes");

            migrationBuilder.DropTable(
                name: "Dividas");

            migrationBuilder.DropTable(
                name: "InteracoesSociais");

            migrationBuilder.DropTable(
                name: "Livros");

            migrationBuilder.DropTable(
                name: "MarcosConcluidos");

            migrationBuilder.DropTable(
                name: "PerfisFinanceiros");

            migrationBuilder.DropTable(
                name: "Habilidades");

            migrationBuilder.DropColumn(
                name: "HabilidadeId",
                table: "SessoesFoco");

            migrationBuilder.DropColumn(
                name: "AvisoFinancasAceitoEm",
                table: "Personagens");
        }
    }
}
