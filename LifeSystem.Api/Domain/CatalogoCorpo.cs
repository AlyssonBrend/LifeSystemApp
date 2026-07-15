namespace LifeSystem.Api.Domain;

// Catálogo e fórmulas da Fase 2 — Corpo (PRD seções 4.2 e 4.3).
// Mesma regra de arquitetura: TODA regra de jogo vive no backend.

public record ExercicioDef(string Id, string Nome, string Emoji, string Grupo, bool Basico);

public record FichaDia(string Nome, string[] Exercicios);

public record FichaDef(string Id, string Nome, string Objetivo, string Frequencia, FichaDia[] Dias);

public record MetasNutricionais(int Calorias, int ProteinaG, int GorduraG, int CarboG, int FibrasG, int AguaMl);

public record Refeicao(string Nome, int Kcal, string Sugestao);

public static class CatalogoCorpo
{
    // Básicos (supino, agachamento, terra) alimentam a fórmula real de Força (PRD 3.1)
    public static readonly ExercicioDef[] Exercicios =
    [
        new("supino", "Supino reto", "🏋️", "Peito", Basico: true),
        new("agachamento", "Agachamento livre", "🦵", "Pernas", Basico: true),
        new("terra", "Levantamento terra", "🏗️", "Costas", Basico: true),
        new("desenvolvimento", "Desenvolvimento", "💪", "Ombros", Basico: false),
        new("remada", "Remada curvada", "🚣", "Costas", Basico: false),
        new("barrafixa", "Barra fixa", "🧗", "Costas", Basico: false),
        new("rosca", "Rosca direta", "💪", "Bíceps", Basico: false),
        new("triceps", "Tríceps na polia", "💪", "Tríceps", Basico: false),
        new("legpress", "Leg press", "🦵", "Pernas", Basico: false),
        new("panturrilha", "Panturrilha em pé", "🦵", "Pernas", Basico: false),
    ];

    // Fichas prontas por objetivo e frequência (PRD 4.3)
    public static readonly FichaDef[] Fichas =
    [
        new("fullbody3x", "Full Body 3×", "Começar a treinar (geral)", "3× por semana",
        [
            new("Treino único (alternar cargas)",
                ["Agachamento livre 3×10", "Supino reto 3×10", "Remada curvada 3×10",
                 "Desenvolvimento 3×12", "Panturrilha em pé 3×15"]),
        ]),
        new("abc", "ABC", "Hipertrofia", "3 a 6× por semana",
        [
            new("A — Peito e tríceps", ["Supino reto 4×8", "Desenvolvimento 3×10", "Tríceps na polia 3×12"]),
            new("B — Costas e bíceps", ["Levantamento terra 4×6", "Barra fixa 3×máx", "Remada curvada 3×10", "Rosca direta 3×12"]),
            new("C — Pernas", ["Agachamento livre 4×8", "Leg press 3×12", "Panturrilha em pé 4×15"]),
        ]),
        new("abcd", "ABCD", "Hipertrofia avançada", "4× por semana",
        [
            new("A — Peito", ["Supino reto 4×8", "Supino reto (pausado) 3×6", "Tríceps na polia 3×12"]),
            new("B — Costas", ["Levantamento terra 4×5", "Barra fixa 4×máx", "Remada curvada 4×10"]),
            new("C — Pernas", ["Agachamento livre 4×8", "Leg press 4×12", "Panturrilha em pé 4×15"]),
            new("D — Ombros e braços", ["Desenvolvimento 4×10", "Rosca direta 3×12", "Tríceps na polia 3×12"]),
        ]),
    ];

    /// <summary>Faixas de PR de corrida em km (PRD 4.3): a corrida entra na maior faixa que cobre.</summary>
    public static readonly int[] FaixasCardioKm = [1, 5, 10, 15, 21, 42];
}

public static class FormulasCorpo
{
    // ---------- Academia (PRD 4.3) ----------

    /// <summary>1RM estimado pela fórmula de Epley: carga × (1 + reps/30).</summary>
    public static double Rm1Epley(double cargaKg, int reps) =>
        reps <= 1 ? cargaKg : cargaKg * (1 + reps / 30.0);

    /// <summary>Pace em segundos por km.</summary>
    public static int PaceSegPorKm(double distanciaKm, double duracaoMin) =>
        (int)Math.Round(duracaoMin * 60 / distanciaKm);

    /// <summary>Maior faixa padrão coberta pela corrida (7 km → faixa de 5 km); null se &lt; 1 km.</summary>
    public static int? FaixaDe(double distanciaKm)
    {
        int? faixa = null;
        foreach (var f in CatalogoCorpo.FaixasCardioKm)
            if (distanciaKm >= f) faixa = f;
        return faixa;
    }

    // ---------- Atributos reais (PRD 3.1, fórmulas da Fase 2) ----------

    /// <summary>
    /// Força pela média do 1RM relativo nos básicos (1RM ÷ peso corporal).
    /// Âncoras do PRD: 1× peso ≈ 50 · 1,5× ≈ 70 · 2× ≈ 90 (linear entre elas).
    /// </summary>
    public static int ForcaDe(double rm1RelativoMedio) => rm1RelativoMedio switch
    {
        <= 0 => 0,
        < 1.0 => (int)Math.Round(rm1RelativoMedio * 50),
        < 1.5 => (int)Math.Round(50 + (rm1RelativoMedio - 1.0) * 40),
        < 2.0 => (int)Math.Round(70 + (rm1RelativoMedio - 1.5) * 40),
        _ => Math.Min(100, (int)Math.Round(90 + (rm1RelativoMedio - 2.0) * 20)),
    };

    /// <summary>
    /// Resistência pelo melhor pace na faixa de 5 km + volume mensal (PRD 3.1).
    /// Âncoras (tempo de 5k): 30min ≈ 40 · 25min ≈ 60 · 20min ≈ 85; volume soma até +15 (10 km/mês = +1).
    /// </summary>
    public static int ResistenciaDe(int melhorPace5kSegKm, double kmUltimos30Dias)
    {
        var tempo5kMin = melhorPace5kSegKm * 5 / 60.0;
        var doPace = tempo5kMin switch
        {
            <= 17 => 100.0,
            <= 20 => 85 + (20 - tempo5kMin) / 3 * 15,
            <= 25 => 60 + (25 - tempo5kMin) / 5 * 25,
            <= 30 => 40 + (30 - tempo5kMin) / 5 * 20,
            <= 40 => 20 + (40 - tempo5kMin) / 10 * 20,
            _ => 20.0,
        };
        var doVolume = Math.Min(15.0, kmUltimos30Dias / 10);
        return Math.Min(100, (int)Math.Round(doPace + doVolume));
    }

    // ---------- Dieta (PRD 4.2, Mifflin-St Jeor) ----------

    public static readonly string[] NiveisAtividade = ["sedentario", "leve", "moderado", "intenso", "atleta"];
    public static readonly string[] Objetivos = ["emagrecer", "manter", "ganhar"];

    public static MetasNutricionais Metas(double pesoKg, int alturaCm, int idade, string sexo, string atividade, string objetivo)
    {
        var tmb = 10 * pesoKg + 6.25 * alturaCm - 5 * idade + (sexo == "f" ? -161 : 5);
        var fator = atividade switch
        {
            "sedentario" => 1.2,
            "leve" => 1.375,
            "moderado" => 1.55,
            "intenso" => 1.725,
            "atleta" => 1.9,
            _ => 1.375,
        };
        var gasto = tmb * fator;
        var calorias = objetivo switch
        {
            "emagrecer" => gasto * 0.80, // déficit de 20%
            "ganhar" => gasto * 1.10,    // superávit de 10%
            _ => gasto,
        };
        // Proteína 1,8–2,2 g/kg (mais alta no déficit, para preservar massa) · gordura 0,9 g/kg · resto em carbo
        var proteina = pesoKg * (objetivo == "emagrecer" ? 2.2 : objetivo == "ganhar" ? 2.0 : 1.8);
        var gordura = pesoKg * 0.9;
        var carbo = Math.Max(0, (calorias - proteina * 4 - gordura * 9) / 4);
        return new MetasNutricionais(
            (int)Math.Round(calorias), (int)Math.Round(proteina), (int)Math.Round(gordura),
            (int)Math.Round(carbo), 30, (int)Math.Round(pesoKg * 35));
    }

    /// <summary>Plano de refeições simples com substituições (Nível 1 — regras determinísticas, PRD seção 4).</summary>
    public static List<Refeicao> PlanoRefeicoes(MetasNutricionais m)
    {
        int Kcal(double fracao) => (int)Math.Round(m.Calorias * fracao);
        return
        [
            new("Café da manhã", Kcal(0.20), "Ovos mexidos + aveia com fruta (troque por tapioca com queijo e um iogurte)"),
            new("Almoço", Kcal(0.30), "Arroz + feijão + frango grelhado + salada (troque o frango por carne magra ou peixe)"),
            new("Lanche", Kcal(0.15), "Iogurte com whey e uma fruta (troque por sanduíche de atum no pão integral)"),
            new("Jantar", Kcal(0.25), "Batata-doce ou arroz + carne + legumes (versão mais leve do almoço)"),
            new("Ceia", Kcal(0.10), "Ovo cozido, queijo ou iogurte proteico — algo leve antes de dormir"),
        ];
    }
}
