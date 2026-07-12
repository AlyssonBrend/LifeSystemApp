using LifeSystem.Api.Data;
using LifeSystem.Api.Domain;
using LifeSystem.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Tests;

public class JogoServiceTests : JogoServiceTestBase
{

    private async Task<ChefeInstancia> ChefeAtual()
    {
        await Jogo.Sincronizar(P);
        await Db.SaveChangesAsync();
        return await Db.ChefesInstancias
            .OrderByDescending(c => c.SemanaInicio)
            .FirstAsync(c => c.PersonagemId == P.Id);
    }

    // ---------- Missões ----------

    [Fact]
    public async Task ConcluirTreinar_PagaXpComMultiplicadorEDanificaOChefe()
    {
        await Jogo.Sincronizar(P);
        var eventos = await Jogo.ConcluirMissao(P, "treinar");
        await Db.SaveChangesAsync();

        Assert.Equal(1, P.StreakDias);            // primeira missão do dia inicia a sequência
        Assert.Equal(255, P.XpAtual);             // 250 × 1,02
        Assert.Equal(26, P.Moedas);               // 255 ÷ 10
        Assert.Equal(680, (await ChefeAtual()).HpAtual); // 800 − 120
        Assert.Empty(eventos);
    }

    [Fact]
    public async Task ConcluirMissaoDuasVezes_NaoPagaDeNovo()
    {
        await Jogo.Sincronizar(P);
        await Jogo.ConcluirMissao(P, "treinar");
        var xp = P.XpAtual;
        await Jogo.ConcluirMissao(P, "treinar");
        Assert.Equal(xp, P.XpAtual);
    }

    [Fact]
    public async Task Checklist_SoConcluiComTodosOsItens()
    {
        await Jogo.Sincronizar(P);
        await Jogo.MarcarCheck(P, "alimentacao", 0, true);
        await Jogo.MarcarCheck(P, "alimentacao", 1, true);
        await Jogo.MarcarCheck(P, "alimentacao", 2, true);
        Assert.Equal(0, P.XpAtual);               // 3 de 4: nada pago ainda

        await Jogo.MarcarCheck(P, "alimentacao", 3, true);
        Assert.Equal(184, P.XpAtual);             // 180 × 1,02
    }

    [Fact]
    public async Task DiaPerfeito_PagaBonusEConcedeProtecao()
    {
        await Jogo.Sincronizar(P);
        var eventos = new List<Contracts.EventoDto>();
        foreach (var m in Catalogo.Missoes)
            eventos.AddRange(await Jogo.ConcluirMissao(P, m.Id));
        await Db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "perfeito");
        Assert.Equal(1, P.DiasPerfeitos);
        Assert.Equal(1, P.ProtecoesStreak);
        Assert.Equal(1, P.Level);                 // ~1.069 XP no dia passa dos 500
        Assert.Contains(eventos, e => e.Tipo == "levelup" && e.Level == 1);
    }

    // ---------- Streak ----------

    [Fact]
    public async Task PularUmDiaSemProtecao_ZeraStreakECuraOChefe()
    {
        await Jogo.Sincronizar(P);
        await Jogo.ConcluirMissao(P, "treinar");
        await Db.SaveChangesAsync();

        Relogio.AvancarDias(2); // um dia inteiro sem missão
        var eventos = await Jogo.Sincronizar(P);
        await Db.SaveChangesAsync();

        Assert.Equal(0, P.StreakDias);
        Assert.Contains(eventos, e => e.Tipo == "chefeCurou");
        Assert.Equal(780, (await ChefeAtual()).HpAtual); // 680 + 100
    }

    [Fact]
    public async Task ProtecaoDeDiaPerfeito_SeguraASequencia()
    {
        await Jogo.Sincronizar(P);
        foreach (var m in Catalogo.Missoes) await Jogo.ConcluirMissao(P, m.Id);
        await Db.SaveChangesAsync();
        Assert.Equal(1, P.ProtecoesStreak);

        Relogio.AvancarDias(2); // pulou um dia — a proteção cobre
        var eventos = await Jogo.Sincronizar(P);
        Assert.Contains(eventos, e => e.Tipo == "streakProtegida");
        Assert.Equal(1, P.StreakDias);
        Assert.Equal(0, P.ProtecoesStreak);

        await Jogo.ConcluirMissao(P, "treinar");
        Assert.Equal(2, P.StreakDias);            // sequência continua de onde estava
    }

    // ---------- Chefe semanal ----------

    [Fact]
    public async Task DerrotarOChefe_PagaRecompensasEDesbloqueiaConquista()
    {
        var chefe = await ChefeAtual();
        chefe.HpAtual = 100; // quase morto
        await Db.SaveChangesAsync();

        var xpAntes = P.XpTotal;
        var eventos = await Jogo.ConcluirMissao(P, "treinar"); // 120 de dano
        await Db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "vitoria");
        Assert.Contains(eventos, e => e.Tipo == "conquista"); // Primeiro sangue
        Assert.Equal(1, P.ChefesDerrotados);
        Assert.True(P.XpTotal >= xpAntes + 1000);
        Assert.Equal("vencida", (await ChefeAtual()).Status);
    }

    [Fact]
    public async Task NaSemanaSeguinteAVitoria_VemOProximoChefeDaRotacao()
    {
        var chefe = await ChefeAtual();
        chefe.HpAtual = 1;
        await Db.SaveChangesAsync();
        await Jogo.ConcluirMissao(P, "treinar");
        await Db.SaveChangesAsync();

        Relogio.AvancarDias(7); // próxima segunda-feira
        var novo = await ChefeAtual();
        Assert.Equal(1, novo.ChefeIndice);         // Preguiça → Procrastinação
        Assert.False(novo.Enfurecido);
        Assert.Equal("ativa", novo.Status);
        Assert.Equal(Formulas.HpChefe(P.Level), novo.HpMax);
    }

    [Fact]
    public async Task SobreviverASemana_TrazOMesmoChefeEnfurecidoCom10PorCentoMaisHp()
    {
        var original = await ChefeAtual();
        Relogio.AvancarDias(7); // semana passou com o chefe vivo

        var novo = await ChefeAtual();
        Assert.Equal(original.ChefeIndice, novo.ChefeIndice);
        Assert.True(novo.Enfurecido);
        Assert.Equal((int)Math.Round(original.HpMax * 1.1), novo.HpMax);
        Assert.Equal("perdida", (await Db.ChefesInstancias.FirstAsync(c => c.Id == original.Id)).Status);
    }

    // ---------- Classes ----------

    [Fact]
    public async Task TrocaDeClasse_RespeitaACarenciaDe30Dias()
    {
        P.Level = 5;
        await Jogo.EscolherClasse(P, "guerreiro");   // primeira escolha: livre
        Assert.Equal("guerreiro", P.Classe);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.EscolherClasse(P, "mago"));

        Relogio.AvancarDias(31);
        await Jogo.EscolherClasse(P, "mago");        // carência cumprida
        Assert.Equal("mago", P.Classe);
    }

    [Fact]
    public async Task ClasseAntesDoLevel5_EBloqueada()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.EscolherClasse(P, "guerreiro"));
    }

    // ---------- Modo Foco ----------

    [Fact]
    public async Task EncerrarFocoAntesDos50Minutos_InvalidaOCiclo()
    {
        await Jogo.Sincronizar(P);
        await Jogo.IniciarFoco(P, "foco");
        await Db.SaveChangesAsync();

        Relogio.AvancarMinutos(20);
        await Jogo.EncerrarFoco(P, abandonar: false);
        await Db.SaveChangesAsync();

        var sessao = await Db.SessoesFoco.SingleAsync();
        Assert.Equal("abandonada", sessao.Status);
        Assert.Equal(0, P.XpAtual);
    }

    [Fact]
    public async Task TresCiclosDeFoco_ConcluemAMissaoEstudar()
    {
        await Jogo.Sincronizar(P);
        for (var ciclo = 0; ciclo < 3; ciclo++)
        {
            await Jogo.IniciarFoco(P, "foco");
            await Db.SaveChangesAsync();
            Relogio.AvancarMinutos(50);
            await Jogo.EncerrarFoco(P, abandonar: false);
            await Db.SaveChangesAsync();
        }

        var log = await Db.MissoesLog.SingleAsync(m => m.MissaoId == "estudar");
        Assert.Equal(150, log.ProgressoMinutos);
        Assert.True(log.Concluida);                 // completou os 120 min no 3º ciclo
        Assert.Equal(3, await Db.SessoesFoco.CountAsync(s => s.Status == "completa"));
    }

    // ---------- Loja ----------

    [Fact]
    public async Task ComprarSemMoedas_EBloqueado()
    {
        var item = P.Loja.First();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.ComprarItem(P, item.Id));
    }

    [Fact]
    public async Task Comprar_DebitaERegistraNoExtrato()
    {
        P.Moedas = 500;
        var item = P.Loja.First(i => i.Preco <= 500);
        await Jogo.ComprarItem(P, item.Id);
        await Db.SaveChangesAsync();

        Assert.Equal(500 - item.Preco, P.Moedas);
        Assert.Single(Db.TransacoesMoedas.Where(t => t.Tipo == "gasto"));
    }
}
