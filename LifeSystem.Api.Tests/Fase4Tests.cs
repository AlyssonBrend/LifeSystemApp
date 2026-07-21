using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Tests;

public class MentorTests : JogoServiceTestBase
{
    [Fact] // sem chave configurada, o módulo avisa e o resto do jogo segue normal
    public async Task SemChave_FalhaComMensagemAmigavel()
    {
        Ia.Configurado = false;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.AnalisarComMentor(P));
        Assert.Contains("não configurada", ex.Message);

        var mentor = await Jogo.MontarMentor(P);
        Assert.False(mentor.Configurado);
        Assert.Empty(mentor.Historico);
    }

    [Fact] // o conselho é persistido (AdviceLog do PRD) e aparece no histórico
    public async Task Analisar_PersisteConselhoNoHistorico()
    {
        var eventos = await Jogo.AnalisarComMentor(P);
        await Db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "mentor");
        Assert.Equal(1, Ia.Chamadas);

        var mentor = await Jogo.MontarMentor(P);
        Assert.True(mentor.Configurado);
        Assert.Single(mentor.Historico);
        Assert.Contains("Mentor", mentor.Historico[0].Conteudo);
        Assert.Equal(mentor.LimiteDiario - 1, mentor.RestantesHoje);
    }

    [Fact] // limite de chamadas/dia por usuário (PRD seção 9); virar o dia reabilita
    public async Task LimiteDiario_BloqueiaEReabilitaNoDiaSeguinte()
    {
        var mentor = await Jogo.MontarMentor(P);
        for (var i = 0; i < mentor.LimiteDiario; i++)
        {
            await Jogo.AnalisarComMentor(P);
            await Db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.AnalisarComMentor(P));

        Relogio.AvancarDias(1);
        await Jogo.AnalisarComMentor(P);
        await Db.SaveChangesAsync();
        Assert.Equal(mentor.LimiteDiario + 1, await Db.ConselhosMentor.CountAsync());
    }

    [Fact] // o contexto enviado à IA reflete os dados reais do jogador — nunca inventados
    public async Task Contexto_ContemDadosReaisDoJogador()
    {
        await Jogo.ConcluirMissao(P, "treinar");
        await Jogo.DefinirPerfilFinanceiro(P, new Contracts.PerfilFinanceiroReq(3000, 1500, 500, 100));
        await Jogo.RegistrarAporte(P, 600);
        await Jogo.CriarHabilidade(P, "csharp", null);
        await Db.SaveChangesAsync();

        await Jogo.AnalisarComMentor(P);

        var ctx = Ia.UltimoContexto!;
        Assert.Contains("Testador", ctx);
        Assert.Contains("Streak atual: 1", ctx);
        Assert.Contains("treinar", ctx);              // missão dos últimos 7 dias
        Assert.Contains("Chefe da semana", ctx);
        Assert.Contains("Finanças: Nível", ctx);
        Assert.Contains("C#", ctx);                   // habilidade da árvore
        Assert.Contains("Disciplina", ctx);           // atributos

        // O prompt de sistema carrega as regras de segurança (PRD 4.5)
        Assert.Contains("Nunca prescreva medicamentos", Ia.UltimoSistema!);
    }

    [Fact] // resposta vazia da IA não vira conselho no histórico
    public async Task RespostaVazia_NaoPersiste()
    {
        Ia.Resposta = "   ";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.AnalisarComMentor(P));
        Assert.Equal(0, await Db.ConselhosMentor.CountAsync());
    }
}
