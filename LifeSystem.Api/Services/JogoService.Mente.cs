using LifeSystem.Api.Contracts;
using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Services;

/// <summary>
/// Fase 3 — Mente (PRD 4.4): árvore de conhecimento (trilhas do catálogo + habilidades
/// personalizadas com marcos por horas), livros e interação social diária (Carisma).
/// </summary>
public partial class JogoService
{
    private const int MarcoXp = 50;
    private const int MarcoMoedas = 5;
    private const int LivroXp = 100;
    private const int LivroMoedas = 10;
    private const int SocialXp = 20;
    private const int SocialMoedas = 2;

    // ---------- Habilidades ----------

    public async Task<List<EventoDto>> CriarHabilidade(Personagem p, string? trilhaId, string? nome)
    {
        Habilidade nova;
        if (trilhaId is not null)
        {
            var trilha = CatalogoMente.Trilhas.FirstOrDefault(t => t.Id == trilhaId)
                ?? throw new ArgumentException("Trilha desconhecida");
            var jaTem = await db.Habilidades.AnyAsync(h => h.PersonagemId == p.Id && h.TrilhaId == trilhaId);
            if (jaTem) throw new InvalidOperationException("Você já segue essa trilha");
            nova = new Habilidade { PersonagemId = p.Id, TrilhaId = trilhaId, Nome = trilha.Nome, Emoji = trilha.Emoji };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Dê um nome à habilidade");
            nova = new Habilidade { PersonagemId = p.Id, Nome = nome.Trim() };
        }
        nova.CriadaEm = relogio.AgoraUtc;
        db.Habilidades.Add(nova);
        return [];
    }

    public async Task<List<EventoDto>> ConcluirMarco(Personagem p, int habilidadeId, int indice)
    {
        var habilidade = await db.Habilidades.Include(h => h.Marcos)
            .FirstOrDefaultAsync(h => h.Id == habilidadeId && h.PersonagemId == p.Id)
            ?? throw new ArgumentException("Habilidade não encontrada");
        if (habilidade.TrilhaId is null)
            throw new InvalidOperationException("Habilidades personalizadas destravam marcos pelas horas de foco");

        var trilha = CatalogoMente.Trilhas.First(t => t.Id == habilidade.TrilhaId);
        if (indice < 0 || indice >= trilha.Marcos.Length) throw new ArgumentException("Marco desconhecido");
        if (habilidade.Marcos.Any(m => m.MarcoIndice == indice)) return [];

        habilidade.Marcos.Add(new MarcoConcluido
        {
            HabilidadeId = habilidade.Id, MarcoIndice = indice, ConcluidoEm = relogio.AgoraUtc,
        });

        var eventos = new List<EventoDto>();
        GanharMoedas(p, MarcoMoedas, $"marco:{habilidade.TrilhaId}:{indice}");
        AplicarXp(p, MarcoXp, eventos);
        eventos.Add(new("marco", Nome: habilidade.Nome, Titulo: trilha.Marcos[indice], Emoji: "🌳"));
        eventos.AddRange(await ChecarConquistasMente(p));
        return eventos;
    }

    // ---------- Livros ----------

    public async Task<List<EventoDto>> CriarLivro(Personagem p, string titulo, int? habilidadeId)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new ArgumentException("Dê um título ao livro");
        if (habilidadeId is { } hid && !await db.Habilidades.AnyAsync(h => h.Id == hid && h.PersonagemId == p.Id))
            throw new ArgumentException("Habilidade não encontrada");
        db.Livros.Add(new Livro
        {
            PersonagemId = p.Id, Titulo = titulo.Trim(), HabilidadeId = habilidadeId,
            CriadoEm = relogio.AgoraUtc,
        });
        return [];
    }

    public async Task<List<EventoDto>> ConcluirLivro(Personagem p, int livroId)
    {
        var livro = await db.Livros
            .FirstOrDefaultAsync(l => l.Id == livroId && l.PersonagemId == p.Id)
            ?? throw new ArgumentException("Livro não encontrado");
        if (livro.ConcluidoEm is not null) return [];

        livro.ConcluidoEm = relogio.AgoraUtc;
        var eventos = new List<EventoDto>();

        // Anti-farm: máx. 1 livro premiado por semana (auto-relato ilimitado viraria fonte de XP);
        // o livro sempre conta para o 📚 Conhecimento — só o prêmio é limitado.
        var segunda = Formulas.SegundaFeiraDe(relogio.Hoje);
        var premiados = await db.Livros
            .Where(l => l.PersonagemId == p.Id && l.Premiado && l.ConcluidoEm != null && l.Id != livro.Id)
            .Select(l => l.ConcluidoEm!.Value)
            .ToListAsync();
        var jaPremiadoNaSemana = premiados.Any(c => relogio.DataDe(c) >= segunda);

        if (!jaPremiadoNaSemana)
        {
            livro.Premiado = true;
            GanharMoedas(p, LivroMoedas, $"livro:{livro.Id}");
            AplicarXp(p, LivroXp, eventos);
            eventos.Add(new("livroConcluido", Nome: livro.Titulo, Emoji: "📕",
                Titulo: $"+{LivroXp} XP · +{LivroMoedas} 🪙 — o Conhecimento agradece"));
        }
        else
        {
            eventos.Add(new("livroConcluido", Nome: livro.Titulo, Emoji: "📕",
                Titulo: "Conta para o Conhecimento! (prêmio já usado esta semana — máx. 1 livro premiado/semana)"));
        }
        eventos.AddRange(await ChecarConquistasMente(p));
        return eventos;
    }

    // ---------- Interação social (Carisma, PRD 3.1) ----------

    public async Task<List<EventoDto>> RegistrarInteracaoSocial(Personagem p)
    {
        var hoje = relogio.Hoje;
        var jaHoje = await db.InteracoesSociais
            .AnyAsync(i => i.PersonagemId == p.Id && i.Data == hoje);
        if (jaHoje) throw new InvalidOperationException("Interação de hoje já registrada");

        db.InteracoesSociais.Add(new InteracaoSocial
        {
            PersonagemId = p.Id, Data = hoje, CriadoEm = relogio.AgoraUtc,
        });
        var eventos = new List<EventoDto>();
        GanharMoedas(p, SocialMoedas, "social");
        AplicarXp(p, SocialXp, eventos);
        eventos.AddRange(await ChecarConquistasMente(p));
        return eventos;
    }

    // ---------- Conquistas da Mente ----------

    private async Task<List<EventoDto>> ChecarConquistasMente(Personagem p)
    {
        var eventos = new List<EventoDto>();
        var desbloqueadas = p.Conquistas.Select(c => c.ConquistaId).ToHashSet();
        var hoje = relogio.Hoje;

        var (livros, marcos) = await ContarLivrosEMarcos(p);
        var inicio30 = hoje.AddDays(-29);
        var diasSociais = await db.InteracoesSociais
                              .CountAsync(i => i.PersonagemId == p.Id && i.Data >= inicio30)
                          + db.InteracoesSociais.Local
                              .Count(i => i.PersonagemId == p.Id && i.Data >= inicio30 && i.Id == 0);

        var checagens = new (string Id, bool Ok)[]
        {
            ("livro1", livros >= 1),
            ("livros10", livros >= 10),
            ("marcos10", marcos >= 10),
            ("social30", diasSociais >= 30),
        };

        foreach (var (id, ok) in checagens)
        {
            if (desbloqueadas.Contains(id) || !ok) continue;
            p.Conquistas.Add(new ConquistaDesbloqueada { PersonagemId = p.Id, ConquistaId = id });
            GanharMoedas(p, 100, $"conquista:{id}");
            var def = Catalogo.Conquistas.First(c => c.Id == id);
            eventos.Add(new("conquista", Nome: def.Nome, Emoji: def.Emoji));
        }
        return eventos;
    }

    /// <summary>
    /// Livros concluídos + marcos da árvore (manuais e por horas) — a fonte da fórmula real
    /// do 📚 Conhecimento (PRD 3.1). Une o change tracker ao banco (registros desta requisição).
    /// </summary>
    private async Task<(int Livros, int Marcos)> ContarLivrosEMarcos(Personagem p)
    {
        // Une o banco ao change tracker: o livro pode ter sido concluído nesta mesma requisição
        var idsConcluidos = (await db.Livros
            .Where(l => l.PersonagemId == p.Id && l.ConcluidoEm != null)
            .Select(l => l.Id)
            .ToListAsync()).ToHashSet();
        var novosLocais = 0;
        foreach (var l in db.Livros.Local.Where(l => l.PersonagemId == p.Id && l.ConcluidoEm != null))
        {
            if (l.Id == 0) novosLocais++;
            else idsConcluidos.Add(l.Id);
        }
        var livros = idsConcluidos.Count + novosLocais;

        var habilidades = await db.Habilidades.Include(h => h.Marcos)
            .Where(h => h.PersonagemId == p.Id)
            .ToListAsync();
        var horasPorHabilidade = await HorasFocoPorHabilidade(p);

        var marcos = 0;
        foreach (var h in habilidades)
        {
            if (h.TrilhaId is not null)
                marcos += h.Marcos.Count;
            else
                // Personalizadas: marcos automáticos por horas de foco (10/25/50/100h)
                marcos += CatalogoMente.MarcosPorHoras.Count(horas => horasPorHabilidade.GetValueOrDefault(h.Id) >= horas);
        }
        return (livros, marcos);
    }

    private async Task<Dictionary<int, double>> HorasFocoPorHabilidade(Personagem p)
    {
        var minutos = await db.SessoesFoco
            .Where(s => s.PersonagemId == p.Id && s.Status == "completa" && s.Tipo == "foco" && s.HabilidadeId != null)
            .GroupBy(s => s.HabilidadeId!.Value)
            .Select(g => new { Id = g.Key, Sessoes = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Sessoes * (double)MinutosFoco);
        return minutos.ToDictionary(kv => kv.Key, kv => kv.Value / 60.0);
    }

    // ---------- Estado do módulo Mente ----------

    public async Task<MenteDto> MontarMente(Personagem p)
    {
        var hoje = relogio.Hoje;
        var habilidades = await db.Habilidades.Include(h => h.Marcos)
            .Where(h => h.PersonagemId == p.Id)
            .OrderBy(h => h.CriadaEm)
            .ToListAsync();
        var horas = await HorasFocoPorHabilidade(p);

        var habilidadesDto = habilidades.Select(h =>
        {
            var horasFoco = Math.Round(horas.GetValueOrDefault(h.Id), 1);
            List<MarcoDto> marcos;
            if (h.TrilhaId is not null)
            {
                var trilha = CatalogoMente.Trilhas.First(t => t.Id == h.TrilhaId);
                var feitos = h.Marcos.Select(m => m.MarcoIndice).ToHashSet();
                marcos = trilha.Marcos.Select((nome, i) => new MarcoDto(i, nome, feitos.Contains(i))).ToList();
            }
            else
            {
                marcos = CatalogoMente.MarcosPorHoras
                    .Select((h2, i) => new MarcoDto(i, $"{h2}h de estudo focado", horasFoco >= h2))
                    .ToList();
            }
            return new HabilidadeDto(h.Id, h.TrilhaId, h.Nome, h.Emoji, horasFoco, marcos);
        }).ToList();

        var trilhasAdicionadas = habilidades.Where(h => h.TrilhaId != null).Select(h => h.TrilhaId!).ToHashSet();
        var trilhas = CatalogoMente.Trilhas
            .Select(t => new TrilhaDto(t.Id, t.Nome, t.Emoji, t.Marcos, trilhasAdicionadas.Contains(t.Id)))
            .ToList();

        var livros = await db.Livros
            .Where(l => l.PersonagemId == p.Id)
            .OrderBy(l => l.ConcluidoEm != null).ThenByDescending(l => l.Id)
            .Take(60).ToListAsync();

        var (totalLivros, totalMarcos) = await ContarLivrosEMarcos(p);
        var interacaoHoje = await db.InteracoesSociais.AnyAsync(i => i.PersonagemId == p.Id && i.Data == hoje);
        var inicio30 = hoje.AddDays(-29);
        var diasSociais30 = await db.InteracoesSociais
            .CountAsync(i => i.PersonagemId == p.Id && i.Data >= inicio30);

        return new MenteDto(
            habilidadesDto,
            trilhas,
            livros.Select(l => new LivroDto(l.Id, l.Titulo, l.HabilidadeId, l.ConcluidoEm != null)).ToList(),
            totalLivros, totalMarcos,
            interacaoHoje, diasSociais30,
            MontarConselhosMente(habilidadesDto, totalLivros, interacaoHoje));
    }

    /// <summary>Conselhos de Nível 1 (PRD 4.4): equilíbrio entre habilidades e uso do Modo Foco.</summary>
    private static List<string> MontarConselhosMente(List<HabilidadeDto> habilidades, int livros, bool interacaoHoje)
    {
        var conselhos = new List<string>();
        if (habilidades.Count == 0)
        {
            conselhos.Add("Adicione uma trilha do catálogo ou crie uma habilidade sua — cada sessão de foco vira progresso na árvore.");
            return conselhos;
        }

        var comFoco = habilidades.Where(h => h.HorasFoco > 0).ToList();
        if (comFoco.Count == 0)
            conselhos.Add("Use o Modo Foco escolhendo uma habilidade — o tempo estudado é creditado à trilha automaticamente.");
        else if (habilidades.Count > 1)
        {
            var dominante = comFoco.MaxBy(h => h.HorasFoco)!;
            var totalHoras = comFoco.Sum(h => h.HorasFoco);
            var negligenciadas = habilidades.Where(h => h.Id != dominante.Id && h.HorasFoco < totalHoras * 0.1).ToList();
            if (totalHoras >= 2 && negligenciadas.Count > 0)
                conselhos.Add($"Quase todo o seu foco foi para {dominante.Nome} — reserve uma sessão para {negligenciadas[0].Nome} e mantenha a árvore equilibrada.");
        }

        if (livros == 0)
            conselhos.Add("Adicione um livro à lista de leitura — livros concluídos alimentam o atributo 📚 Conhecimento.");
        if (!interacaoHoje)
            conselhos.Add("Registre a interação social de hoje (um contato de qualidade) — constância alimenta o 🤝 Carisma.");

        return conselhos;
    }
}
