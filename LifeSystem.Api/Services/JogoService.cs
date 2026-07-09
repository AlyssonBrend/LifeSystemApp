using System.Text.Json;
using LifeSystem.Api.Contracts;
using LifeSystem.Api.Data;
using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Services;

/// <summary>
/// Todas as regras de jogo (PRD seção 6: nunca no frontend).
/// Cada ação sincroniza streak/chefe/foco com a data real antes de executar.
/// </summary>
public class JogoService(AppDb db)
{
    private const int MinutosFoco = 50;
    private const int MinutosDescanso = 10;
    private const int ToleranciaSegundos = 30;

    // ---------- Carregamento e sincronização ----------

    public async Task<Personagem> CarregarPersonagem(int usuarioId)
    {
        var p = await db.Personagens
            .Include(x => x.Conquistas)
            .Include(x => x.Loja.Where(i => i.Ativo))
            .FirstAsync(x => x.UsuarioId == usuarioId);
        return p;
    }

    /// <summary>Ajusta o mundo à data real: quebra de streak, chefe da semana, ciclos de foco vencidos.</summary>
    public async Task<List<EventoDto>> Sincronizar(Personagem p)
    {
        var eventos = new List<EventoDto>();
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var ontem = hoje.AddDays(-1);

        var chefe = await ObterChefeDaSemana(p, hoje);

        // Quebrar a sequência não tira XP, mas zera o contador e o chefe recupera 100 HP (PRD 3.3)
        if (p.StreakDias > 0 && p.UltimoDiaComMissao is { } ultimo && ultimo < ontem)
        {
            p.StreakDias = 0;
            if (chefe.Status == "ativa")
            {
                chefe.HpAtual = Math.Min(chefe.HpMax, chefe.HpAtual + 100);
                eventos.Add(new("chefeCurou"));
            }
        }

        // Ciclo de foco que já passou do alvo: completa sozinho (timestamps do servidor, PRD 3.9)
        var sessao = await db.SessoesFoco
            .FirstOrDefaultAsync(s => s.PersonagemId == p.Id && s.Status == "ativa");
        if (sessao is not null)
        {
            var alvo = TimeSpan.FromMinutes(sessao.Tipo == "foco" ? MinutosFoco : MinutosDescanso);
            if (DateTime.UtcNow - sessao.IniciadaEm >= alvo)
                eventos.AddRange(await CompletarSessaoFoco(p, sessao, chefe));
        }

        return eventos;
    }

    private async Task<ChefeInstancia> ObterChefeDaSemana(Personagem p, DateOnly hoje)
    {
        var segunda = Formulas.SegundaFeiraDe(hoje);
        var atual = await db.ChefesInstancias
            .FirstOrDefaultAsync(c => c.PersonagemId == p.Id && c.SemanaInicio == segunda);
        if (atual is not null) return atual;

        var anterior = await db.ChefesInstancias
            .Where(c => c.PersonagemId == p.Id)
            .OrderByDescending(c => c.SemanaInicio)
            .FirstOrDefaultAsync();

        int indice = 0;
        bool enfurecido = false;
        int hpMax = Formulas.HpChefe(p.Level);

        if (anterior is not null)
        {
            if (anterior.Status == "ativa")
            {
                // Derrota (PRD 3.4): sem punição; o chefe volta "Enfurecido" com +10% HP
                anterior.Status = "perdida";
                indice = anterior.ChefeIndice;
                enfurecido = true;
                hpMax = (int)Math.Round(anterior.HpMax * 1.1);
            }
            else
            {
                indice = (anterior.ChefeIndice + 1) % Catalogo.Chefes.Length;
            }
        }

        var novo = new ChefeInstancia
        {
            PersonagemId = p.Id,
            ChefeIndice = indice,
            SemanaInicio = segunda,
            HpMax = hpMax,
            HpAtual = hpMax,
            Enfurecido = enfurecido,
        };
        db.ChefesInstancias.Add(novo);
        return novo;
    }

    // ---------- Estado ----------

    public async Task<EstadoDto> MontarEstado(Personagem p)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var chefe = await ObterChefeDaSemana(p, hoje);
        var logsHoje = await db.MissoesLog
            .Where(m => m.PersonagemId == p.Id && m.Data == hoje)
            .ToListAsync();
        var atributos = await CalcularAtributos(p, hoje);
        var mult = Formulas.MultiplicadorStreak(p.StreakDias);
        var bonusAtivo = BonusClasseAtivo(p, atributos);

        var missoes = Catalogo.Missoes.Select(def =>
        {
            var log = logsHoje.FirstOrDefault(l => l.MissaoId == def.Id);
            var checks = LerChecks(def, log);
            var comBonus = TemBonusClasse(p, def) && bonusAtivo;
            var xpFinal = (int)Math.Round(def.Xp * mult * (comBonus ? 1.2 : 1));
            return new MissaoDto(
                def.Id, def.Nome, def.Emoji, def.Requisito,
                def.Xp, xpFinal, Formulas.MoedasDeXp(xpFinal), def.DanoChefe,
                log?.Concluida ?? false, comBonus,
                def.Checklist, checks,
                def.MinutosNecessarios, log?.ProgressoMinutos ?? 0);
        }).ToList();

        var dormiuOntem = await db.MissoesLog.AnyAsync(m =>
            m.PersonagemId == p.Id && m.MissaoId == "dormir" && m.Data == hoje.AddDays(-1) && m.Concluida);
        var vit = atributos.First(a => a.Id == "vitalidade").Valor;
        var rec = atributos.First(a => a.Id == "recuperacao").Valor;
        var hp = Math.Min(100, 50 + (vit + rec) / 2);

        var defChefe = Catalogo.Chefes[chefe.ChefeIndice];
        var conquistasIds = p.Conquistas.Select(c => c.ConquistaId).ToHashSet();

        var compras = await db.TransacoesMoedas
            .Where(t => t.PersonagemId == p.Id && t.Tipo == "gasto")
            .OrderByDescending(t => t.CriadoEm)
            .Take(8)
            .Select(t => new CompraDto(t.Origem.Replace("loja:", ""), t.Valor, t.CriadoEm))
            .ToListAsync();

        var sessao = await db.SessoesFoco
            .FirstOrDefaultAsync(s => s.PersonagemId == p.Id && s.Status == "ativa");
        FocoDto? foco = sessao is null ? null : new(
            sessao.Tipo, sessao.IniciadaEm,
            (sessao.Tipo == "foco" ? MinutosFoco : MinutosDescanso) * 60,
            (int)(DateTime.UtcNow - sessao.IniciadaEm).TotalSeconds);

        return new EstadoDto(
            new PersonagemDto(
                p.Nome, p.Level, p.XpAtual, Formulas.XpParaProximoLevel(p.Level), p.XpTotal,
                p.Moedas, p.Economias, p.StreakDias, mult,
                p.Classe, Formulas.TituloAtual(p.Level, p.Classe), Formulas.EmojiTitulo(p.Level, p.Classe),
                hp, dormiuOntem ? 95 : 72, p.DiasPerfeitos, p.ChefesDerrotados,
                p.Level >= 5 && p.Classe is null, bonusAtivo),
            atributos,
            missoes,
            new ChefeDto(
                chefe.Enfurecido ? $"{defChefe.Nome} Enfurecida" : defChefe.Nome,
                defChefe.Emoji, defChefe.Ataques,
                chefe.HpAtual, chefe.HpMax, chefe.Enfurecido, chefe.Status,
                p.RecompensaCaixa, Formulas.SegundaFeiraDe(hoje).AddDays(7).ToString("yyyy-MM-dd")),
            Catalogo.Conquistas
                .Select(c => new ConquistaDto(c.Id, c.Nome, c.Emoji, c.Descricao, conquistasIds.Contains(c.Id)))
                .ToList(),
            p.Loja.Where(i => i.Ativo).Select(i => new ItemLojaDto(i.Id, i.Nome, i.Preco)).ToList(),
            compras,
            foco,
            DateTime.UtcNow);
    }

    // ---------- Atributos: proxy de consistência (PRD 3.1, MVP) ----------

    private async Task<List<AtributoDto>> CalcularAtributos(Personagem p, DateOnly hoje)
    {
        var inicio30 = hoje.AddDays(-29);
        var inicio90 = hoje.AddDays(-89);

        var logs90 = await db.MissoesLog
            .Where(m => m.PersonagemId == p.Id && m.Data >= inicio90)
            .ToListAsync();
        var logs30 = logs90.Where(m => m.Data >= inicio30).ToList();

        int Conta30(string missao) => logs30.Count(m => m.MissaoId == missao && m.Concluida);
        int PctConsistencia30(string missao) => Math.Min(100, (int)Math.Round(Conta30(missao) / 30.0 * 100));

        // Inteligência: horas de estudo/mês medidas pelo Modo Foco (60h/mês = 100)
        var minutosEstudo30 = logs30.Where(m => m.MissaoId == "estudar").Sum(m => m.ProgressoMinutos);
        var inteligencia = Math.Min(100, (int)Math.Round(minutosEstudo30 / 60.0 / 60.0 * 100));

        // Conhecimento: proxy por volume acumulado de estudo em 90 dias (100h = 100)
        var minutosEstudo90 = logs90.Where(m => m.MissaoId == "estudar").Sum(m => m.ProgressoMinutos);
        var conhecimento = Math.Min(100, (int)Math.Round(minutosEstudo90 / 60.0 / 100.0 * 100));

        // Disciplina (atributo central): streak atual + taxa de conclusão na janela de 90 dias
        var diasJogados = Math.Max(1, logs90.Select(m => m.Data).Distinct().Count());
        var taxaConclusao = logs90.Count(m => m.Concluida) / (double)(diasJogados * Catalogo.Missoes.Length) * 100;
        var streakPts = Math.Min(100.0, p.StreakDias / 30.0 * 100);
        var disciplina = (int)Math.Round(0.6 * taxaConclusao + 0.4 * streakPts);

        var valores = new Dictionary<string, int>
        {
            ["forca"] = PctConsistencia30("treinar"),
            ["resistencia"] = PctConsistencia30("treinar"),
            ["vitalidade"] = PctConsistencia30("alimentacao"),
            ["recuperacao"] = PctConsistencia30("dormir"),
            ["inteligencia"] = inteligencia,
            ["conhecimento"] = conhecimento,
            ["espirito"] = PctConsistencia30("espiritualidade"),
            ["financas"] = PctConsistencia30("trabalhar"),
            ["carisma"] = 0, // fonte de dados chega na Fase 3
            ["disciplina"] = disciplina,
        };

        return Catalogo.Atributos.Select(a =>
        {
            var v = valores[a.Id];
            return new AtributoDto(a.Id, a.Nome, a.Emoji, v, Formulas.FaixaAtributo(v), a.TemDadosNoMvp);
        }).ToList();
    }

    private static bool TemBonusClasse(Personagem p, MissaoDef def)
    {
        if (p.Classe is null || p.Level < 5) return false;
        var classe = Catalogo.Classes.First(c => c.Id == p.Classe);
        return def.Atributos.Any(a => classe.Primarias.Contains(a));
    }

    /// <summary>Piso de manutenção (PRD 3.5): atributo do piso abaixo do mínimo suspende o bônus.</summary>
    private static bool BonusClasseAtivo(Personagem p, List<AtributoDto> atributos)
    {
        if (p.Classe is null || p.Level < 5) return false;
        var classe = Catalogo.Classes.First(c => c.Id == p.Classe);
        return classe.Pisos.All(piso => atributos.First(a => a.Id == piso.Attr).Valor >= piso.Minimo
            // durante o primeiro mês os proxies ainda estão "aquecendo" — não punir de cara
            || p.Level < 8);
    }

    // ---------- Missões ----------

    public async Task<List<EventoDto>> ConcluirMissao(Personagem p, string missaoId)
    {
        var def = Catalogo.Missoes.FirstOrDefault(m => m.Id == missaoId)
            ?? throw new ArgumentException("Missão desconhecida");
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var log = await ObterLog(p, def, hoje);
        var eventos = new List<EventoDto>();
        if (log.Concluida) return eventos;

        if (def.MinutosNecessarios is { } minutos)
            log.ProgressoMinutos = minutos; // registro manual completa as 2 horas

        if (def.Checklist is not null)
            log.ChecksJson = JsonSerializer.Serialize(def.Checklist.Select(_ => true).ToArray());

        await ConcluirInterna(p, def, log, eventos);
        return eventos;
    }

    public async Task<List<EventoDto>> MarcarCheck(Personagem p, string missaoId, int indice, bool marcado)
    {
        var def = Catalogo.Missoes.FirstOrDefault(m => m.Id == missaoId && m.Checklist is not null)
            ?? throw new ArgumentException("Missão sem checklist");
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var log = await ObterLog(p, def, hoje);
        var eventos = new List<EventoDto>();
        if (log.Concluida || indice < 0 || indice >= def.Checklist!.Length) return eventos;

        var checks = LerChecks(def, log);
        checks[indice] = marcado;
        log.ChecksJson = JsonSerializer.Serialize(checks);

        if (checks.All(c => c))
            await ConcluirInterna(p, def, log, eventos);
        return eventos;
    }

    private async Task<MissaoLog> ObterLog(Personagem p, MissaoDef def, DateOnly data)
    {
        var log = await db.MissoesLog
            .FirstOrDefaultAsync(m => m.PersonagemId == p.Id && m.MissaoId == def.Id && m.Data == data);
        if (log is null)
        {
            log = new MissaoLog { PersonagemId = p.Id, MissaoId = def.Id, Data = data };
            db.MissoesLog.Add(log);
        }
        return log;
    }

    private static bool[] LerChecks(MissaoDef def, MissaoLog? log)
    {
        var tamanho = def.Checklist?.Length ?? 0;
        if (tamanho == 0) return [];
        var salvos = log is null ? [] : JsonSerializer.Deserialize<bool[]>(log.ChecksJson) ?? [];
        var checks = new bool[tamanho];
        Array.Copy(salvos, checks, Math.Min(salvos.Length, tamanho));
        return checks;
    }

    private async Task ConcluirInterna(Personagem p, MissaoDef def, MissaoLog log, List<EventoDto> eventos)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        log.Concluida = true;
        log.ConcluidaEm = DateTime.UtcNow;

        // Streak: dias consecutivos com ≥1 missão concluída (PRD 3.3)
        if (p.UltimoDiaComMissao != hoje)
        {
            p.StreakDias = p.UltimoDiaComMissao == hoje.AddDays(-1) ? p.StreakDias + 1 : 1;
            p.UltimoDiaComMissao = hoje;
            eventos.AddRange(await ChecarConquistas(p));
        }

        var atributos = await CalcularAtributos(p, hoje);
        var mult = Formulas.MultiplicadorStreak(p.StreakDias);
        var comBonus = TemBonusClasse(p, def) && BonusClasseAtivo(p, atributos);
        var xp = (int)Math.Round(def.Xp * mult * (comBonus ? 1.2 : 1));

        GanharMoedas(p, Formulas.MoedasDeXp(xp), $"missao:{def.Id}");
        AplicarXp(p, xp, eventos);

        // Dano no chefe da semana (PRD 3.4)
        var chefe = await ObterChefeDaSemana(p, hoje);
        if (def.DanoChefe > 0 && chefe.Status == "ativa")
        {
            chefe.HpAtual = Math.Max(0, chefe.HpAtual - def.DanoChefe);
            if (chefe.HpAtual == 0)
            {
                chefe.Status = "vencida";
                p.ChefesDerrotados++;
                GanharMoedas(p, 200, "chefe");
                AplicarXp(p, 1000, eventos);
                var defChefe = Catalogo.Chefes[chefe.ChefeIndice];
                eventos.Add(new("vitoria", Nome: defChefe.Nome, Emoji: defChefe.Emoji, Recompensa: p.RecompensaCaixa));
            }
        }

        // Bônus de dia perfeito (PRD 3.3): +100 XP, +20 moedas
        var concluidasHoje = await db.MissoesLog
            .CountAsync(m => m.PersonagemId == p.Id && m.Data == hoje && m.Concluida);
        // o log atual pode ainda não estar salvo — garante a contagem local
        concluidasHoje = Math.Max(concluidasHoje, db.ChangeTracker.Entries<MissaoLog>()
            .Count(e => e.Entity.PersonagemId == p.Id && e.Entity.Data == hoje && e.Entity.Concluida));
        if (concluidasHoje == Catalogo.Missoes.Length)
        {
            p.DiasPerfeitos++;
            GanharMoedas(p, 20, "diaPerfeito");
            AplicarXp(p, 100, eventos);
            eventos.Add(new("perfeito"));
        }

        eventos.AddRange(await ChecarConquistas(p));
    }

    // ---------- XP / moedas / conquistas ----------

    private void AplicarXp(Personagem p, int quantidade, List<EventoDto> eventos)
    {
        p.XpAtual += quantidade;
        p.XpTotal += quantidade;
        while (p.XpAtual >= Formulas.XpParaProximoLevel(p.Level))
        {
            p.XpAtual -= Formulas.XpParaProximoLevel(p.Level);
            p.Level++;
            GanharMoedas(p, 50, "levelup");
            eventos.Add(new("levelup", Titulo: Formulas.TituloAtual(p.Level, p.Classe), Level: p.Level));
        }
    }

    private void GanharMoedas(Personagem p, int valor, string origem)
    {
        p.Moedas += valor;
        db.TransacoesMoedas.Add(new TransacaoMoedas
        {
            PersonagemId = p.Id, Tipo = "ganho", Valor = valor, Origem = origem,
        });
    }

    private async Task<List<EventoDto>> ChecarConquistas(Personagem p)
    {
        var eventos = new List<EventoDto>();
        var desbloqueadas = p.Conquistas.Select(c => c.ConquistaId).ToHashSet();

        int TotalMissao(string id) => db.MissoesLog
            .Count(m => m.PersonagemId == p.Id && m.MissaoId == id && m.Concluida);
        var horasEstudo = await db.MissoesLog
            .Where(m => m.PersonagemId == p.Id && m.MissaoId == "estudar")
            .SumAsync(m => m.ProgressoMinutos) / 60.0;

        var checagens = new (string Id, Func<bool> Ok)[]
        {
            ("streak7", () => p.StreakDias >= 7),
            ("streak30", () => p.StreakDias >= 30),
            ("streak100", () => p.StreakDias >= 100),
            ("treino30", () => TotalMissao("treinar") >= 30),
            ("treino100", () => TotalMissao("treinar") >= 100),
            ("refeicoes100", () => TotalMissao("alimentacao") >= 100),
            ("estudo100h", () => horasEstudo >= 100),
            ("chefe1", () => p.ChefesDerrotados >= 1),
            ("chefe10", () => p.ChefesDerrotados >= 10),
            ("poupanca10k", () => p.Economias >= 10000),
        };

        foreach (var (id, ok) in checagens)
        {
            if (desbloqueadas.Contains(id) || !ok()) continue;
            p.Conquistas.Add(new ConquistaDesbloqueada { PersonagemId = p.Id, ConquistaId = id });
            GanharMoedas(p, 100, $"conquista:{id}");
            var def = Catalogo.Conquistas.First(c => c.Id == id);
            eventos.Add(new("conquista", Nome: def.Nome, Emoji: def.Emoji));
        }
        return eventos;
    }

    // ---------- Modo Foco 50/10 (PRD 3.9) ----------

    public async Task<List<EventoDto>> IniciarFoco(Personagem p, string tipo)
    {
        if (tipo is not ("foco" or "descanso")) throw new ArgumentException("Tipo inválido");
        var ativa = await db.SessoesFoco
            .AnyAsync(s => s.PersonagemId == p.Id && s.Status == "ativa");
        if (ativa) throw new InvalidOperationException("Já existe uma sessão ativa");
        db.SessoesFoco.Add(new SessaoFoco
        {
            PersonagemId = p.Id, Tipo = tipo, IniciadaEm = DateTime.UtcNow,
        });
        return [];
    }

    public async Task<List<EventoDto>> EncerrarFoco(Personagem p, bool abandonar)
    {
        var sessao = await db.SessoesFoco
            .FirstOrDefaultAsync(s => s.PersonagemId == p.Id && s.Status == "ativa")
            ?? throw new InvalidOperationException("Nenhuma sessão ativa");

        var alvoMinutos = sessao.Tipo == "foco" ? MinutosFoco : MinutosDescanso;
        var decorrido = DateTime.UtcNow - sessao.IniciadaEm;

        if (abandonar || decorrido < TimeSpan.FromMinutes(alvoMinutos) - TimeSpan.FromSeconds(ToleranciaSegundos))
        {
            // Abandono invalida o ciclo — sem punição, ele só não conta (PRD 3.9)
            sessao.Status = "abandonada";
            sessao.EncerradaEm = DateTime.UtcNow;
            return [];
        }

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var chefe = await ObterChefeDaSemana(p, hoje);
        return await CompletarSessaoFoco(p, sessao, chefe);
    }

    private async Task<List<EventoDto>> CompletarSessaoFoco(Personagem p, SessaoFoco sessao, ChefeInstancia chefe)
    {
        var eventos = new List<EventoDto>();
        sessao.Status = "completa";
        sessao.EncerradaEm = DateTime.UtcNow;
        if (sessao.Tipo != "foco") return eventos;

        // Cada ciclo de 50min alimenta a missão Estudar e paga bônus de foco (+10 XP, +1 moeda)
        var defEstudar = Catalogo.Missoes.First(m => m.Id == "estudar");
        var data = DateOnly.FromDateTime(sessao.IniciadaEm.ToLocalTime());
        var log = await ObterLog(p, defEstudar, data);
        log.ProgressoMinutos += MinutosFoco;
        GanharMoedas(p, 1, "focoCiclo");
        AplicarXp(p, 10, eventos);
        eventos.Add(new("focoCompleto"));

        if (!log.Concluida && log.ProgressoMinutos >= defEstudar.MinutosNecessarios)
            await ConcluirInterna(p, defEstudar, log, eventos);
        return eventos;
    }

    // ---------- Classe / loja / demais ações ----------

    public Task<List<EventoDto>> EscolherClasse(Personagem p, string classeId)
    {
        if (p.Level < 5) throw new InvalidOperationException("A classe é escolhida no level 5");
        if (Catalogo.Classes.All(c => c.Id != classeId)) throw new ArgumentException("Classe desconhecida");
        p.Classe = classeId;
        return Task.FromResult(new List<EventoDto> { new("classe", Nome: classeId) });
    }

    public async Task<List<EventoDto>> ComprarItem(Personagem p, int itemId)
    {
        var item = await db.ItensLoja
            .FirstOrDefaultAsync(i => i.Id == itemId && i.PersonagemId == p.Id && i.Ativo)
            ?? throw new ArgumentException("Item não encontrado");
        if (p.Moedas < item.Preco) throw new InvalidOperationException("Moedas insuficientes");
        p.Moedas -= item.Preco;
        db.TransacoesMoedas.Add(new TransacaoMoedas
        {
            PersonagemId = p.Id, Tipo = "gasto", Valor = item.Preco, Origem = $"loja:{item.Nome}",
        });
        return [new("compra", Nome: item.Nome)];
    }

    public Task<List<EventoDto>> AdicionarItemLoja(Personagem p, string nome, int preco)
    {
        if (string.IsNullOrWhiteSpace(nome) || preco <= 0) throw new ArgumentException("Item inválido");
        db.ItensLoja.Add(new ItemLoja { PersonagemId = p.Id, Nome = nome.Trim(), Preco = preco });
        return Task.FromResult(new List<EventoDto>());
    }

    public Task<List<EventoDto>> DefinirRecompensaCaixa(Personagem p, string texto)
    {
        p.RecompensaCaixa = texto.Trim();
        return Task.FromResult(new List<EventoDto>());
    }

    public async Task<List<EventoDto>> DefinirEconomias(Personagem p, decimal valor)
    {
        p.Economias = Math.Max(0, valor);
        return await ChecarConquistas(p);
    }

    // ---------- Criação de personagem ----------

    public static Personagem NovoPersonagem(int usuarioId, string nome)
    {
        var p = new Personagem
        {
            UsuarioId = usuarioId,
            Nome = nome,
            Level = 0, // todo mundo parte do zero (PRD 3.2)
            RecompensaCaixa = "1 episódio da série da vez",
        };
        foreach (var (nomeItem, preco) in Catalogo.LojaInicial)
            p.Loja.Add(new ItemLoja { Nome = nomeItem, Preco = preco });
        return p;
    }
}
