namespace LifeSystem.Api.Domain;

// Catálogo estático das regras do PRD (seções 3.1–3.8).
// Regra de arquitetura do PRD (seção 6): TODA regra de jogo vive aqui no backend.

public record MissaoDef(string Id, string Nome, string Emoji, string Requisito, int Xp, int DanoChefe, string[] Atributos, string[]? Checklist = null, int? MinutosNecessarios = null);

public record ClasseDef(string Id, string Nome, string Emoji, string[] Primarias, string[] Titulos, string Lema, (string Attr, int Minimo)[] Pisos);

public record ChefeDef(string Nome, string Emoji, string[] Ataques);

public record ConquistaDef(string Id, string Nome, string Emoji, string Descricao, bool Oculta = false);

public record AtributoDef(string Id, string Nome, string Emoji, bool TemDadosNoMvp);

public static class Catalogo
{
    // Tabela 3.3 (XP e dano do chefe da tabela 3.4; moedas = XP÷10)
    public static readonly MissaoDef[] Missoes =
    [
        new("treinar", "Treinar", "🏋️", "1 treino registrado", 250, 120, ["forca", "resistencia"]),
        new("alimentacao", "Alimentação", "🍗", "Checklist nutricional completo", 180, 100, ["vitalidade"],
            Checklist: ["200g de proteína", "30g de fibras", "2 frutas", "3L de água"]),
        new("estudar", "Estudar", "📖", "2 horas (Modo Foco ou registro manual)", 200, 80, ["inteligencia"],
            MinutosNecessarios: 120),
        new("espiritualidade", "Espiritualidade", "🙏", "Práticas do dia completas", 120, 50, ["espirito"],
            Checklist: ["Bíblia 20min", "Oração 15min", "Gratidão 5min"]),
        new("trabalhar", "Trabalhar", "💼", "8 horas de trabalho", 150, 0, ["financas"]),
        new("dormir", "Dormir cedo", "😴", "Registrar sono antes das 23h", 50, 150, ["recuperacao"]),
    ];

    /// <summary>Ids das 6 missões padrão — dia perfeito e taxa de conclusão contam só estas.</summary>
    public static readonly HashSet<string> IdsMissoesPadrao = Missoes.Select(m => m.Id).ToHashSet();

    // Seção 3.5: 1 missão diária extra temática por classe (paga o bônus de primárias por definição)
    public static readonly Dictionary<string, MissaoDef> MissoesDeClasse = new()
    {
        ["guerreiro"] = new("classe-guerreiro", "Treino de força", "⚔️", "Missão de classe: sessão dedicada de força", 150, 0, ["forca"]),
        ["ranger"] = new("classe-ranger", "Corrida ou trilha", "🏹", "Missão de classe: corrida, trilha ou caminhada longa", 150, 0, ["resistencia"]),
        ["mago"] = new("classe-mago", "Leitura profunda", "🧙", "Missão de classe: 30+ min de leitura sem distrações", 150, 0, ["conhecimento", "inteligencia"]),
        ["monge"] = new("classe-monge", "Meditação longa", "🧘", "Missão de classe: 20+ min de meditação ou silêncio", 150, 0, ["espirito", "disciplina"]),
        ["paladino"] = new("classe-paladino", "Oração e ferro", "🛡️", "Missão de classe: oração + treino no mesmo dia", 150, 0, ["espirito", "forca"]),
        ["mercador"] = new("classe-mercador", "Revisar as finanças", "💰", "Missão de classe: revisar gastos e metas do dia", 150, 0, ["financas"]),
    };

    // Seção 3.5 — classes com títulos por tier e pisos de manutenção
    public static readonly ClasseDef[] Classes =
    [
        new("guerreiro", "Guerreiro", "⚔️", ["forca", "resistencia"],
            ["Escudeiro", "Guerreiro", "Cavaleiro", "Campeão", "Senhor da Guerra"],
            "Músculo sem mente é só uma arma sem punho.", [("inteligencia", 30), ("vitalidade", 40)]),
        new("ranger", "Ranger", "🏹", ["resistencia", "vitalidade"],
            ["Batedor", "Ranger", "Caçador", "Andarilho", "Lorde das Trilhas"],
            "A trilha recompensa quem volta todo dia.", [("forca", 30), ("conhecimento", 25)]),
        new("mago", "Mago", "🧙", ["inteligencia", "conhecimento"],
            ["Aprendiz", "Mago", "Feiticeiro", "Arquimago", "Arcano"],
            "A torre não sustenta um corpo em ruínas.", [("vitalidade", 40), ("forca", 30)]),
        new("monge", "Monge", "🧘", ["espirito", "disciplina"],
            ["Noviço", "Monge", "Asceta", "Ancião", "Iluminado"],
            "Silêncio por fora, constância por dentro.", [("vitalidade", 40), ("carisma", 25)]),
        new("paladino", "Paladino", "🛡️", ["espirito", "forca"],
            ["Escudeiro da Fé", "Paladino", "Cruzado", "Templário", "Guardião Sagrado"],
            "Fé e ferro, temperados pelo descanso.", [("inteligencia", 30), ("recuperacao", 35)]),
        new("mercador", "Mercador", "💰", ["financas", "carisma"],
            ["Ambulante", "Mercador", "Magnata", "Barão", "Rei Mercador"],
            "Ouro que não descansa vira burnout.", [("recuperacao", 40), ("forca", 25)]),
    ];

    // Rotação de chefes do MVP (seção 3.4)
    public static readonly ChefeDef[] Chefes =
    [
        new("Preguiça", "👹", ["ficar na cama", "redes sociais", "comida ruim", "desistir"]),
        new("Procrastinação", "🐌", ["\"depois eu faço\"", "scroll infinito", "planejar sem executar"]),
        new("Gula", "🍩", ["besteira à noite", "açúcar escondido", "pular a refeição de verdade"]),
        new("Distração", "📱", ["notificações", "abas infinitas", "YouTube \"só 5 minutos\""]),
        new("Desânimo", "🌧️", ["\"não vai dar certo\"", "comparação", "cansaço mental"]),
    ];

    // Seção 3.6
    public static readonly ConquistaDef[] Conquistas =
    [
        new("streak7", "Uma semana de fogo", "🔥", "7 dias de sequência"),
        new("streak30", "Um mês imparável", "🔥", "30 dias de sequência"),
        new("streak100", "Lendário", "🔥", "100 dias de sequência"),
        new("treino30", "Corpo em construção", "🏋️", "30 dias treinando"),
        new("treino100", "Ferro e vontade", "🏋️", "100 dias sem faltar treino"),
        new("refeicoes100", "Combustível limpo", "🍎", "100 refeições saudáveis"),
        new("estudo100h", "Mente afiada", "📚", "100 horas estudadas"),
        new("chefe1", "Primeiro sangue", "👹", "Primeiro chefe derrotado"),
        new("chefe10", "Caçador de fraquezas", "👹", "10 chefes derrotados"),
        new("poupanca10k", "Primeiros £10.000", "💰", "£10.000 economizados"),
        // A surpresa é parte da recompensa (PRD 3.5): aparece como ??? até despertar
        new("semPontosFracos", "Sem Pontos Fracos", "✨", "Despertou o Avatar Transcendente", Oculta: true),
    ];

    // Seção 3.1 — os 10 atributos; os sem fonte de dados no MVP exibem proxy zerado
    public static readonly AtributoDef[] Atributos =
    [
        new("disciplina", "Disciplina", "⚡", true),
        new("forca", "Força", "💪", true),
        new("resistencia", "Resistência", "🏃", true),
        new("vitalidade", "Vitalidade", "❤️", true),
        new("recuperacao", "Recuperação", "😴", true),
        new("inteligencia", "Inteligência", "🧠", true),
        new("conhecimento", "Conhecimento", "📚", true),
        new("espirito", "Espírito", "🙏", true),
        new("financas", "Finanças", "💰", true),
        new("carisma", "Carisma", "🤝", false), // fonte de dados só na Fase 3
    ];

    // Loja inicial (exemplos do PRD 3.8)
    public static readonly (string Nome, int Preco)[] LojaInicial =
    [
        ("1 episódio de série", 50),
        ("Sobremesa livre", 80),
        ("Jogar 2h de videogame", 100),
        ("Jantar fora", 300),
        ("Comprar algo da wishlist", 800),
    ];
}

public static class Formulas
{
    /// <summary>Seção 3.2: XP para subir do level N para N+1.</summary>
    public static int XpParaProximoLevel(int level) => 500 + level * 200;

    /// <summary>Seção 3.2: multiplicador de disciplina — +2%/dia de streak, teto +40%.</summary>
    public static double MultiplicadorStreak(int streakDias) => Math.Min(1.4, 1 + 0.02 * streakDias);

    /// <summary>Seção 3.4: HP do chefe escala com o level do jogador.</summary>
    public static int HpChefe(int level) => 800 + level * 100;

    /// <summary>Seção 3.8: moedas = XP da missão ÷ 10.</summary>
    public static int MoedasDeXp(int xp) => (int)Math.Round(xp / 10.0);

    /// <summary>Seção 3.1: faixas da escala universal 0–100.</summary>
    public static string FaixaAtributo(int v) => v switch
    {
        >= 95 => "Lendário",
        >= 80 => "Elite",
        >= 60 => "Avançado",
        >= 40 => "Intermediário",
        >= 20 => "Casual",
        _ => "Iniciante",
    };

    public static string TituloAtual(int level, string? classeId, bool transcendente = false)
    {
        if (transcendente) return "Avatar Transcendente";
        if (level < 5 || classeId is null) return "Aldeão";
        var classe = Catalogo.Classes.First(c => c.Id == classeId);
        var tier = level switch { >= 50 => 4, >= 35 => 3, >= 20 => 2, >= 10 => 1, _ => 0 };
        return classe.Titulos[tier];
    }

    public static string EmojiTitulo(int level, string? classeId, bool transcendente = false) =>
        transcendente ? "✨"
        : level < 5 || classeId is null ? "🌾"
        : Catalogo.Classes.First(c => c.Id == classeId).Emoji;

    /// <summary>Segunda-feira da semana de uma data (chefes surgem toda segunda, seção 3.4).</summary>
    public static DateOnly SegundaFeiraDe(DateOnly data)
    {
        int diff = ((int)data.DayOfWeek + 6) % 7; // Monday=0 ... Sunday=6
        return data.AddDays(-diff);
    }
}
