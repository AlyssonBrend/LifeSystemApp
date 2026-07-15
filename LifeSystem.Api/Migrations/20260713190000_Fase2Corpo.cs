using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fase2Corpo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvisoSaudeAceitoEm",
                table: "Personagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoAmigo",
                table: "Personagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RankingOptIn",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Amizades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConvidadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amizades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfisCorporais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PesoKg = table.Column<double>(type: "REAL", nullable: false),
                    AlturaCm = table.Column<int>(type: "INTEGER", nullable: false),
                    Idade = table.Column<int>(type: "INTEGER", nullable: false),
                    Sexo = table.Column<string>(type: "TEXT", nullable: false),
                    Atividade = table.Column<string>(type: "TEXT", nullable: false),
                    Objetivo = table.Column<string>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisCorporais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfisCorporais_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosCardio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanciaKm = table.Column<double>(type: "REAL", nullable: false),
                    DuracaoMin = table.Column<double>(type: "REAL", nullable: false),
                    PaceSegKm = table.Column<int>(type: "INTEGER", nullable: false),
                    FaixaKm = table.Column<int>(type: "INTEGER", nullable: true),
                    Pr = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrPremiado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosCardio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosCarga",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExercicioId = table.Column<string>(type: "TEXT", nullable: false),
                    CargaKg = table.Column<double>(type: "REAL", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false),
                    Rm1 = table.Column<double>(type: "REAL", nullable: false),
                    Pr = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrPremiado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosCarga", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_CodigoAmigo",
                table: "Personagens",
                column: "CodigoAmigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Amizades_SolicitanteId_ConvidadoId",
                table: "Amizades",
                columns: new[] { "SolicitanteId", "ConvidadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfisCorporais_PersonagemId",
                table: "PerfisCorporais",
                column: "PersonagemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCardio_PersonagemId_Data",
                table: "RegistrosCardio",
                columns: new[] { "PersonagemId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCarga_PersonagemId_ExercicioId_Data",
                table: "RegistrosCarga",
                columns: new[] { "PersonagemId", "ExercicioId", "Data" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Amizades");

            migrationBuilder.DropTable(
                name: "PerfisCorporais");

            migrationBuilder.DropTable(
                name: "RegistrosCardio");

            migrationBuilder.DropTable(
                name: "RegistrosCarga");

            migrationBuilder.DropIndex(
                name: "IX_Personagens_CodigoAmigo",
                table: "Personagens");

            migrationBuilder.DropColumn(
                name: "AvisoSaudeAceitoEm",
                table: "Personagens");

            migrationBuilder.DropColumn(
                name: "CodigoAmigo",
                table: "Personagens");

            migrationBuilder.DropColumn(
                name: "RankingOptIn",
                table: "Personagens");
        }
    }
}
