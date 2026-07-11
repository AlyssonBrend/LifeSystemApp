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

public class JogoServiceTests : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly AppDb _db;
    private readonly RelogioFake _relogio = new();
    private readonly JogoService _jogo;
    private readonly Personagem _p;

    public JogoServiceTests()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();
        _db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(_conexao).Options);
        _db.Database.EnsureCreated();

        var usuario = new Usuario { Email = "teste@teste.com", SenhaHash = "x" };
        _db.Usuarios.Add(usuario);
        _db.SaveChanges();
        _p = JogoService.NovoPersonagem(usuario.Id, "Testador");
        _db.Personagens.Add(_p);
        _db.SaveChanges();

        _jogo = new JogoService(_db, _relogio);
    }

    public void Dispose() => _conexao.Dispose();

    private async Task<ChefeInstancia> ChefeAtual()
    {
        await _jogo.Sincronizar(_p);
        await _db.SaveChangesAsync();
        return await _db.ChefesInstancias
            .OrderByDescending(c => c.SemanaInicio)
            .FirstAsync(c => c.PersonagemId == _p.Id);
    }

    // ---------- Missões ----------

    [Fact]
    public async Task ConcluirTreinar_PagaXpComMultiplicadorEDanificaOChefe()
    {
        await _jogo.Sincronizar(_p);
        var eventos = await _jogo.ConcluirMissao(_p, "treinar");
        await _db.SaveChangesAsync();

        Assert.Equal(1, _p.StreakDias);            // primeira missão do dia inicia a sequência
        Assert.Equal(255, _p.XpAtual);             // 250 × 1,02
        Assert.Equal(26, _p.Moedas);               // 255 ÷ 10
        Assert.Equal(680, (await ChefeAtual()).HpAtual); // 800 − 120
        Assert.Empty(eventos);
    }

    [Fact]
    public async Task ConcluirMissaoDuasVezes_NaoPagaDeNovo()
    {
        await _jogo.Sincronizar(_p);
        await _jogo.ConcluirMissao(_p, "treinar");
        var xp = _p.XpAtual;
        await _jogo.ConcluirMissao(_p, "treinar");
        Assert.Equal(xp, _p.XpAtual);
    }

    [Fact]
    public async Task Checklist_SoConcluiComTodosOsItens()
    {
        await _jogo.Sincronizar(_p);
        await _jogo.MarcarCheck(_p, "alimentacao", 0, true);
        await _jogo.MarcarCheck(_p, "alimentacao", 1, true);
        await _jogo.MarcarCheck(_p, "alimentacao", 2, true);
        Assert.Equal(0, _p.XpAtual);               // 3 de 4: nada pago ainda

        await _jogo.MarcarCheck(_p, "alimentacao", 3, true);
        Assert.Equal(184, _p.XpAtual);             // 180 × 1,02
    }

    [Fact]
    public async Task DiaPerfeito_PagaBonusEConcedeProtecao()
    {
        await _jogo.Sincronizar(_p);
        var eventos = new List<Contracts.EventoDto>();
        foreach (var m in Catalogo.Missoes)
            eventos.AddRange(await _jogo.ConcluirMissao(_p, m.Id));
        await _db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "perfeito");
        Assert.Equal(1, _p.DiasPerfeitos);
        Assert.Equal(1, _p.ProtecoesStreak);
        Assert.Equal(1, _p.Level);                 // ~1.069 XP no dia passa dos 500
        Assert.Contains(eventos, e => e.Tipo == "levelup" && e.Level == 1);
    }

    // ---------- Streak ----------

    [Fact]
    public async Task PularUmDiaSemProtecao_ZeraStreakECuraOChefe()
    {
        await _jogo.Sincronizar(_p);
        await _jogo.ConcluirMissao(_p, "treinar");
        await _db.SaveChangesAsync();

        _relogio.AvancarDias(2); // um dia inteiro sem missão
        var eventos = await _jogo.Sincronizar(_p);
        await _db.SaveChangesAsync();

        Assert.Equal(0, _p.StreakDias);
        Assert.Contains(eventos, e => e.Tipo == "chefeCurou");
        Assert.Equal(780, (await ChefeAtual()).HpAtual); // 680 + 100
    }

    [Fact]
    public async Task ProtecaoDeDiaPerfeito_SeguraASequencia()
    {
        await _jogo.Sincronizar(_p);
        foreach (var m in Catalogo.Missoes) await _jogo.ConcluirMissao(_p, m.Id);
        await _db.SaveChangesAsync();
        Assert.Equal(1, _p.ProtecoesStreak);

        _relogio.AvancarDias(2); // pulou um dia — a proteção cobre
        var eventos = await _jogo.Sincronizar(_p);
        Assert.Contains(eventos, e => e.Tipo == "streakProtegida");
        Assert.Equal(1, _p.StreakDias);
        Assert.Equal(0, _p.ProtecoesStreak);

        await _jogo.ConcluirMissao(_p, "treinar");
        Assert.Equal(2, _p.StreakDias);            // sequência continua de onde estava
    }

    // ---------- Chefe semanal ----------

    [Fact]
    public async Task DerrotarOChefe_PagaRecompensasEDesbloqueiaConquista()
    {
        var chefe = await ChefeAtual();
        chefe.HpAtual = 100; // quase morto
        await _db.SaveChangesAsync();

        var xpAntes = _p.XpTotal;
        var eventos = await _jogo.ConcluirMissao(_p, "treinar"); // 120 de dano
        await _db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "vitoria");
        Assert.Contains(eventos, e => e.Tipo == "conquista"); // Primeiro sangue
        Assert.Equal(1, _p.ChefesDerrotados);
        Assert.True(_p.XpTotal >= xpAntes + 1000);
        Assert.Equal("vencida", (await ChefeAtual()).Status);
    }

    [Fact]
    public async Task NaSemanaSeguinteAVitoria_VemOProximoChefeDaRotacao()
    {
        var chefe = await ChefeAtual();
        chefe.HpAtual = 1;
        await _db.SaveChangesAsync();
        await _jogo.ConcluirMissao(_p, "treinar");
        await _db.SaveChangesAsync();

        _relogio.AvancarDias(7); // próxima segunda-feira
        var novo = await ChefeAtual();
        Assert.Equal(1, novo.ChefeIndice);         // Preguiça → Procrastinação
        Assert.False(novo.Enfurecido);
        Assert.Equal("ativa", novo.Status);
        Assert.Equal(Formulas.HpChefe(_p.Level), novo.HpMax);
    }

    [Fact]
    public async Task SobreviverASemana_TrazOMesmoChefeEnfurecidoCom10PorCentoMaisHp()
    {
        var original = await ChefeAtual();
        _relogio.AvancarDias(7); // semana passou com o chefe vivo

        var novo = await ChefeAtual();
        Assert.Equal(original.ChefeIndice, novo.ChefeIndice);
        Assert.True(novo.Enfurecido);
        Assert.Equal((int)Math.Round(original.HpMax * 1.1), novo.HpMax);
        Assert.Equal("perdida", (await _db.ChefesInstancias.FirstAsync(c => c.Id == original.Id)).Status);
    }

    // ---------- Classes ----------

    [Fact]
    public async Task TrocaDeClasse_RespeitaACarenciaDe30Dias()
    {
        _p.Level = 5;
        await _jogo.EscolherClasse(_p, "guerreiro");   // primeira escolha: livre
        Assert.Equal("guerreiro", _p.Classe);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _jogo.EscolherClasse(_p, "mago"));

        _relogio.AvancarDias(31);
        await _jogo.EscolherClasse(_p, "mago");        // carência cumprida
        Assert.Equal("mago", _p.Classe);
    }

    [Fact]
    public async Task ClasseAntesDoLevel5_EBloqueada()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _jogo.EscolherClasse(_p, "guerreiro"));
    }

    // ---------- Modo Foco ----------

    [Fact]
    public async Task EncerrarFocoAntesDos50Minutos_InvalidaOCiclo()
    {
        await _jogo.Sincronizar(_p);
        await _jogo.IniciarFoco(_p, "foco");
        await _db.SaveChangesAsync();

        _relogio.AvancarMinutos(20);
        await _jogo.EncerrarFoco(_p, abandonar: false);
        await _db.SaveChangesAsync();

        var sessao = await _db.SessoesFoco.SingleAsync();
        Assert.Equal("abandonada", sessao.Status);
        Assert.Equal(0, _p.XpAtual);
    }

    [Fact]
    public async Task TresCiclosDeFoco_ConcluemAMissaoEstudar()
    {
        await _jogo.Sincronizar(_p);
        for (var ciclo = 0; ciclo < 3; ciclo++)
        {
            await _jogo.IniciarFoco(_p, "foco");
            await _db.SaveChangesAsync();
            _relogio.AvancarMinutos(50);
            await _jogo.EncerrarFoco(_p, abandonar: false);
            await _db.SaveChangesAsync();
        }

        var log = await _db.MissoesLog.SingleAsync(m => m.MissaoId == "estudar");
        Assert.Equal(150, log.ProgressoMinutos);
        Assert.True(log.Concluida);                 // completou os 120 min no 3º ciclo
        Assert.Equal(3, await _db.SessoesFoco.CountAsync(s => s.Status == "completa"));
    }

    // ---------- Loja ----------

    [Fact]
    public async Task ComprarSemMoedas_EBloqueado()
    {
        var item = _p.Loja.First();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _jogo.ComprarItem(_p, item.Id));
    }

    [Fact]
    public async Task Comprar_DebitaERegistraNoExtrato()
    {
        _p.Moedas = 500;
        var item = _p.Loja.First(i => i.Preco <= 500);
        await _jogo.ComprarItem(_p, item.Id);
        await _db.SaveChangesAsync();

        Assert.Equal(500 - item.Preco, _p.Moedas);
        Assert.Single(_db.TransacoesMoedas.Where(t => t.Tipo == "gasto"));
    }
}
