using LifeSystem.Api.Domain;

namespace LifeSystem.Api.Tests;

public class FormulasTests
{
    [Theory] // tabela da seção 3.2 do PRD
    [InlineData(0, 500)]
    [InlineData(1, 700)]
    [InlineData(2, 900)]
    [InlineData(5, 1500)]
    [InlineData(10, 2500)]
    [InlineData(20, 4500)]
    public void CurvaDeXp_SegueOPrd(int level, int esperado) =>
        Assert.Equal(esperado, Formulas.XpParaProximoLevel(level));

    [Theory] // +2%/dia, teto +40% (20 dias)
    [InlineData(0, 1.0)]
    [InlineData(1, 1.02)]
    [InlineData(10, 1.2)]
    [InlineData(20, 1.4)]
    [InlineData(50, 1.4)]
    public void MultiplicadorStreak_TemTeto(int streak, double esperado) =>
        Assert.Equal(esperado, Formulas.MultiplicadorStreak(streak), precision: 10);

    [Theory] // HP = 800 + level × 100 (seção 3.4)
    [InlineData(0, 800)]
    [InlineData(6, 1400)]
    public void HpDoChefe_EscalaComOLevel(int level, int esperado) =>
        Assert.Equal(esperado, Formulas.HpChefe(level));

    [Theory] // moedas = XP ÷ 10 (seção 3.8)
    [InlineData(250, 25)]
    [InlineData(255, 26)]
    [InlineData(51, 5)]
    public void Moedas_SaoXpDivididoPorDez(int xp, int esperado) =>
        Assert.Equal(esperado, Formulas.MoedasDeXp(xp));

    [Theory] // faixas da escala 0–100 (seção 3.1)
    [InlineData(0, "Iniciante")]
    [InlineData(19, "Iniciante")]
    [InlineData(20, "Casual")]
    [InlineData(40, "Intermediário")]
    [InlineData(60, "Avançado")]
    [InlineData(80, "Elite")]
    [InlineData(95, "Lendário")]
    [InlineData(100, "Lendário")]
    public void FaixasDeAtributo_SeguemAEscalaUniversal(int valor, string esperada) =>
        Assert.Equal(esperada, Formulas.FaixaAtributo(valor));

    [Fact]
    public void SegundaFeira_DeQualquerDiaDaSemana()
    {
        var segunda = new DateOnly(2026, 7, 6);
        for (var d = 0; d < 7; d++)
            Assert.Equal(segunda, Formulas.SegundaFeiraDe(segunda.AddDays(d)));
        Assert.Equal(segunda.AddDays(7), Formulas.SegundaFeiraDe(segunda.AddDays(7)));
    }

    [Theory] // títulos por tier (seção 3.5)
    [InlineData(0, null, "Aldeão")]
    [InlineData(7, null, "Aldeão")]
    [InlineData(5, "guerreiro", "Escudeiro")]
    [InlineData(10, "guerreiro", "Guerreiro")]
    [InlineData(20, "guerreiro", "Cavaleiro")]
    [InlineData(35, "mago", "Arquimago")]
    [InlineData(50, "mercador", "Rei Mercador")]
    public void Titulos_PorLevelEClasse(int level, string? classe, string esperado) =>
        Assert.Equal(esperado, Formulas.TituloAtual(level, classe));
}
