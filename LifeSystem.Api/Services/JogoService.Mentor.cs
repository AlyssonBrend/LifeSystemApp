using System.Text;
using LifeSystem.Api.Contracts;
using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Services;

/// <summary>
/// Fase 4 — IA Mentora (PRD 4.5): a API Claude recebe o contexto real do jogador e
/// personaliza os conselhos de Nível 1. Limite de chamadas/dia (PRD seção 9) e
/// histórico auditável em ConselhoMentor. O jogo funciona 100% sem a IA.
/// </summary>
public partial class JogoService
{
    private const int LimiteDiarioMentor = 5; // custo de API sob controle (PRD seção 9)

    private const string SistemaMentor =
        """
        Você é o Mentor do Life System, um RPG da vida real onde hábitos geram XP e
        vícios são chefes semanais. Você recebe o estado real do jogador e devolve uma
        análise personalizada em português do Brasil.

        Regras:
        - Baseie-se APENAS nos dados fornecidos; nunca invente números ou histórico.
        - Estruture a resposta em markdown com exatamente estas seções:
          "## ⚔️ Análise da semana", "## 🎯 Conselhos" (3 a 5 itens acionáveis),
          "## 👹 Desafio" (1 desafio concreto para os próximos 7 dias) e
          "## 🔥 Palavra do Mentor" (mensagem motivacional curta baseada no histórico real).
        - Tom de mestre de RPG: direto, encorajador, sem bajulação — aponte fraquezas
          com respeito e conecte cada conselho aos dados (ex.: streak, atributo baixo).
        - Nunca prescreva medicamentos, dietas restritivas ou investimentos específicos;
          para saúde e dinheiro, reforce os princípios do jogo (consistência, 50/30/20,
          reserva de emergência) e recomende profissionais quando o tema for sério.
        - Máximo de ~350 palavras.
        """;

    // ---------- Ação: analisar a semana ----------

    public async Task<List<EventoDto>> AnalisarComMentor(Personagem p)
    {
        if (!ia.Configurado)
            throw new InvalidOperationException(
                "IA Mentora não configurada — defina Ia:Chave (user-secrets) ou ANTHROPIC_API_KEY e reinicie a API");

        var hoje = relogio.Hoje;
        var usadasHoje = await db.ConselhosMentor
            .CountAsync(c => c.PersonagemId == p.Id && c.Data == hoje);
        if (usadasHoje >= LimiteDiarioMentor)
            throw new InvalidOperationException(
                $"Limite diário do Mentor atingido ({LimiteDiarioMentor} análises/dia) — volte amanhã");

        var contexto = await MontarContextoMentor(p);
        var conteudo = await ia.Gerar(SistemaMentor, contexto);
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new InvalidOperationException("A IA não retornou conselho — tente de novo");

        db.ConselhosMentor.Add(new ConselhoMentor
        {
            PersonagemId = p.Id, Conteudo = conteudo, Data = hoje, CriadoEm = relogio.AgoraUtc,
        });
        return [new("mentor")];
    }

    /// <summary>Contexto real do jogador (PRD 4.5): histórico, quedas de streak, chefe atual…</summary>
    internal async Task<string> MontarContextoMentor(Personagem p)
    {
        var hoje = relogio.Hoje;
        var inicio7 = hoje.AddDays(-6);
        var ctx = new StringBuilder();

        ctx.AppendLine($"# Personagem: {p.Nome}");
        ctx.AppendLine($"Level {p.Level} · Título: {Formulas.TituloAtual(p.Level, p.Classe, p.AvatarTranscendente)}" +
                       $" · Classe: {p.Classe ?? "Aldeão (sem classe)"}");
        ctx.AppendLine($"Streak atual: {p.StreakDias} dias · Dias perfeitos: {p.DiasPerfeitos}" +
                       $" · Chefes derrotados: {p.ChefesDerrotados} · Moedas: {p.Moedas}");

        ctx.AppendLine("\n## Atributos (0-100, medições reais)");
        foreach (var a in await CalcularAtributos(p, hoje))
            ctx.AppendLine($"- {a.Nome}: {a.Valor} ({a.Faixa})");

        var chefe = await ObterChefeDaSemana(p, hoje);
        var defChefe = Catalogo.Chefes[chefe.ChefeIndice];
        ctx.AppendLine($"\n## Chefe da semana: {defChefe.Nome}{(chefe.Enfurecido ? " (Enfurecida)" : "")}" +
                       $" — HP {chefe.HpAtual}/{chefe.HpMax} · status: {chefe.Status}");

        ctx.AppendLine("\n## Missões dos últimos 7 dias (concluídas por dia)");
        var logs7 = await db.MissoesLog
            .Where(m => m.PersonagemId == p.Id && m.Data >= inicio7)
            .ToListAsync();
        for (var d = inicio7; d <= hoje; d = d.AddDays(1))
        {
            var doDia = logs7.Where(m => m.Data == d && m.Concluida).Select(m => m.MissaoId).ToList();
            ctx.AppendLine($"- {d:dd/MM}: {(doDia.Count == 0 ? "nenhuma" : string.Join(", ", doDia))}");
        }

        if (p.PerfilFinanceiro is { } fin && fin.RendaMensal > 0)
        {
            var diag = FormulasFinancas.Diagnostico(fin, p.Economias, await AportesDoMes(p, hoje));
            ctx.AppendLine($"\n## Finanças: Nível {diag.Nivel} (score {diag.Score})" +
                           $" · reserva cobre {diag.MesesReserva:0.#} meses (meta 6)" +
                           $" · poupança do mês {diag.TaxaPoupancaPct:0.#}% (meta 20%)" +
                           $" · economias £{p.Economias:#,0}");
            var dividas = await db.Dividas
                .Where(d => d.PersonagemId == p.Id && d.QuitadaEm == null).ToListAsync();
            if (dividas.Count > 0)
                ctx.AppendLine("Dívidas abertas: " + string.Join("; ",
                    dividas.Select(d => $"{d.Nome} £{d.ValorAtual:#,0} ({d.JurosPctMes:0.#}%/mês)")));
        }

        var (livros, marcos) = await ContarLivrosEMarcos(p);
        var habilidades = await db.Habilidades
            .Where(h => h.PersonagemId == p.Id).Select(h => h.Nome).ToListAsync();
        ctx.AppendLine($"\n## Mente: {livros} livros concluídos · {marcos} marcos da árvore" +
                       $" · habilidades: {(habilidades.Count == 0 ? "nenhuma" : string.Join(", ", habilidades))}");

        var minutosFoco7 = await db.SessoesFoco
            .Where(s => s.PersonagemId == p.Id && s.Status == "completa" && s.Tipo == "foco"
                        && s.IniciadaEm >= relogio.AgoraUtc.AddDays(-7))
            .CountAsync() * MinutosFoco;
        ctx.AppendLine($"Modo Foco nos últimos 7 dias: {minutosFoco7} minutos");

        return ctx.ToString();
    }

    // ---------- Estado do módulo Mentor ----------

    public async Task<MentorDto> MontarMentor(Personagem p)
    {
        var hoje = relogio.Hoje;
        var usadasHoje = await db.ConselhosMentor
            .CountAsync(c => c.PersonagemId == p.Id && c.Data == hoje);
        var historico = await db.ConselhosMentor
            .Where(c => c.PersonagemId == p.Id)
            .OrderByDescending(c => c.CriadoEm)
            .Take(10)
            .Select(c => new ConselhoMentorDto(c.Id, c.Conteudo, c.CriadoEm))
            .ToListAsync();

        return new MentorDto(
            ia.Configurado, LimiteDiarioMentor,
            Math.Max(0, LimiteDiarioMentor - usadasHoje),
            historico);
    }
}
