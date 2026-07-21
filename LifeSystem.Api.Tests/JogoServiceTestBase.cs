using LifeSystem.Api.Data;
using LifeSystem.Api.Domain;
using LifeSystem.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Tests;

/// <summary>Relógio controlável: os testes viajam no tempo em vez de esperar dias reais.</summary>
public class RelogioFake : IRelogio
{
    // Segunda-feira, meio-dia em Brasília (15h UTC)
    public DateTime AgoraUtc { get; set; } = new(2026, 7, 6, 15, 0, 0, DateTimeKind.Utc);
    public DateOnly Hoje => DataDe(AgoraUtc);
    public DateOnly DataDe(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(-3)); // BRT fixo
    public void AvancarDias(int dias) => AgoraUtc = AgoraUtc.AddDays(dias);
    public void AvancarMinutos(int minutos) => AgoraUtc = AgoraUtc.AddMinutes(minutos);
}

/// <summary>IA fake (Fase 4): registra o contexto recebido e devolve uma resposta fixa — sem rede.</summary>
public class ClienteIaFake : IClienteIa
{
    public bool Configurado { get; set; } = true;
    public string Resposta { get; set; } = "## ⚔️ Análise da semana\nConselho de teste do Mentor.";
    public string? UltimoSistema { get; private set; }
    public string? UltimoContexto { get; private set; }
    public int Chamadas { get; private set; }

    public Task<string> Gerar(string sistema, string contexto)
    {
        Chamadas++;
        UltimoSistema = sistema;
        UltimoContexto = contexto;
        return Task.FromResult(Resposta);
    }
}

/// <summary>Banco SQLite in-memory + personagem novo por teste.</summary>
public abstract class JogoServiceTestBase : IDisposable
{
    private readonly SqliteConnection _conexao;
    protected readonly AppDb Db;
    protected readonly RelogioFake Relogio = new();
    protected readonly ClienteIaFake Ia = new();
    protected readonly JogoService Jogo;
    protected readonly Personagem P;

    protected JogoServiceTestBase()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();
        Db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(_conexao).Options);
        Db.Database.EnsureCreated();

        var usuario = new Usuario { Email = "teste@teste.com", SenhaHash = "x" };
        Db.Usuarios.Add(usuario);
        Db.SaveChanges();
        P = JogoService.NovoPersonagem(usuario.Id, "Testador");
        Db.Personagens.Add(P);
        Db.SaveChanges();

        Jogo = new JogoService(Db, Relogio, Ia);
    }

    public void Dispose() => _conexao.Dispose();
}
