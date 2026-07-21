namespace LifeSystem.Api.Domain;

// Fórmulas da Fase 3 — Bolso (PRD 4.1 e 3.8).
// Mesma regra de arquitetura: TODA regra de jogo vive no backend.

public record DiagnosticoFinanceiro(
    double MesesReserva, decimal ReservaMeta, decimal ReservaFaltante,
    double TaxaPoupancaPct, double PctNecessidades, double PctDesejos,
    int Score, string Nivel);

public static class FormulasFinancas
{
    /// <summary>Meta de reserva de emergência: 6× as despesas mensais (PRD 4.1).</summary>
    public const int MesesReservaMeta = 6;

    /// <summary>Meta 50/30/20: poupar 20% da renda (PRD 4.1) — bater a meta é a missão mensal.</summary>
    public const double MetaPoupancaPct = 20;

    /// <summary>Conversão de moedas (PRD 3.8): 10 🪙 = £1, com teto no orçamento de recompensa.</summary>
    public const int MoedasPorLibra = 10;

    /// <summary>
    /// Score do Nível Financeiro (PRD 4.1): taxa de poupança + reserva, escala 0–100.
    /// Poupar 20% ≈ 50 pts · reserva de 6 meses ≈ 50 pts.
    /// </summary>
    public static int Score(double taxaPoupancaPct, double mesesReserva) =>
        Math.Min(100, (int)Math.Round(
            Math.Min(50, taxaPoupancaPct * 2.5) + Math.Min(50, mesesReserva / MesesReservaMeta * 50)));

    /// <summary>Nível Financeiro do personagem: E → D → C → B → A → S (PRD 4.1).</summary>
    public static string Nivel(int score) => score switch
    {
        >= 80 => "S",
        >= 65 => "A",
        >= 50 => "B",
        >= 35 => "C",
        >= 20 => "D",
        _ => "E",
    };

    public static DiagnosticoFinanceiro Diagnostico(
        PerfilFinanceiro perfil, decimal economias, decimal aportesDoMes)
    {
        var despesas = perfil.DespesasFixas + perfil.DespesasVariaveis;
        var mesesReserva = despesas > 0 ? (double)(economias / despesas) : 0;
        var reservaMeta = despesas * MesesReservaMeta;
        var renda = perfil.RendaMensal;

        var taxaPoupanca = renda > 0 ? (double)(Math.Max(0, aportesDoMes) / renda) * 100 : 0;
        // 50/30/20: necessidades = fixas; poupança = aportes do mês; desejos = o resto da renda
        var pctNecessidades = renda > 0 ? (double)(perfil.DespesasFixas / renda) * 100 : 0;
        var pctDesejos = Math.Max(0, 100 - pctNecessidades - taxaPoupanca);

        var score = Score(taxaPoupanca, mesesReserva);
        return new DiagnosticoFinanceiro(
            Math.Round(mesesReserva, 1), reservaMeta, Math.Max(0, reservaMeta - economias),
            Math.Round(taxaPoupanca, 1), Math.Round(pctNecessidades, 1), Math.Round(pctDesejos, 1),
            score, Nivel(score));
    }
}
