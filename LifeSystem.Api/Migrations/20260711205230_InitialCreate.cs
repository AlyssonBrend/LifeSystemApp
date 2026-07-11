using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    XpAtual = table.Column<int>(type: "INTEGER", nullable: false),
                    XpTotal = table.Column<long>(type: "INTEGER", nullable: false),
                    Moedas = table.Column<int>(type: "INTEGER", nullable: false),
                    Economias = table.Column<decimal>(type: "TEXT", nullable: false),
                    StreakDias = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoDiaComMissao = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ProtecoesStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    Classe = table.Column<string>(type: "TEXT", nullable: true),
                    ClasseEscolhidaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecompensaCaixa = table.Column<string>(type: "TEXT", nullable: false),
                    DiasPerfeitos = table.Column<int>(type: "INTEGER", nullable: false),
                    ChefesDerrotados = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personagens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChefesInstancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChefeIndice = table.Column<int>(type: "INTEGER", nullable: false),
                    SemanaInicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HpAtual = table.Column<int>(type: "INTEGER", nullable: false),
                    HpMax = table.Column<int>(type: "INTEGER", nullable: false),
                    Enfurecido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChefesInstancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChefesInstancias_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConquistasDesbloqueadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConquistaId = table.Column<string>(type: "TEXT", nullable: false),
                    DesbloqueadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConquistasDesbloqueadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConquistasDesbloqueadas_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensLoja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Preco = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensLoja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensLoja_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissoesLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    MissaoId = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Concluida = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChecksJson = table.Column<string>(type: "TEXT", nullable: false),
                    ProgressoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcluidaEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissoesLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissoesLog_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessoesFoco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    IniciadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EncerradaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesFoco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesFoco_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransacoesMoedas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<int>(type: "INTEGER", nullable: false),
                    Origem = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransacoesMoedas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransacoesMoedas_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChefesInstancias_PersonagemId_SemanaInicio",
                table: "ChefesInstancias",
                columns: new[] { "PersonagemId", "SemanaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConquistasDesbloqueadas_PersonagemId_ConquistaId",
                table: "ConquistasDesbloqueadas",
                columns: new[] { "PersonagemId", "ConquistaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensLoja_PersonagemId",
                table: "ItensLoja",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_MissoesLog_PersonagemId_MissaoId_Data",
                table: "MissoesLog",
                columns: new[] { "PersonagemId", "MissaoId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_UsuarioId",
                table: "Personagens",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesFoco_PersonagemId",
                table: "SessoesFoco",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_TransacoesMoedas_PersonagemId",
                table: "TransacoesMoedas",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChefesInstancias");

            migrationBuilder.DropTable(
                name: "ConquistasDesbloqueadas");

            migrationBuilder.DropTable(
                name: "ItensLoja");

            migrationBuilder.DropTable(
                name: "MissoesLog");

            migrationBuilder.DropTable(
                name: "SessoesFoco");

            migrationBuilder.DropTable(
                name: "TransacoesMoedas");

            migrationBuilder.DropTable(
                name: "Personagens");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
