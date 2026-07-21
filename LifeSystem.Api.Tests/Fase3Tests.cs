using LifeSystem.Api.Contracts;
using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Tests;

public class FormulasFinancasTests
{
    [Theory] // poupar 20% ≈ 50 pts · reserva de 6 meses ≈ 50 pts (PRD 4.1)
    [InlineData(0, 0, 0)]
    [InlineData(10, 0, 25)]
    [InlineData(20, 0, 50)]
    [InlineData(0, 6, 50)]
    [InlineData(20, 6, 100)]
    [InlineData(40, 12, 100)] // ambos os lados saturam
    public void Score_CombinaPoupancaEReserva(double taxa, double meses, int esperado) =>
        Assert.Equal(esperado, FormulasFinancas.Score(taxa, meses));

    [Theory] // Nível Financeiro E → S (PRD 4.1)
    [InlineData(10, "E")]
    [InlineData(20, "D")]
    [InlineData(35, "C")]
    [InlineData(50, "B")]
    [InlineData(65, "A")]
    [InlineData(80, "S")]
    public void Nivel_SegueAsFaixas(int score, string esperado) =>
        Assert.Equal(esperado, FormulasFinancas.Nivel(score));

    [Fact] // renda 3000, despesas 1500+500, economias 4000, aportes 600 → o exemplo canônico
    public void Diagnostico_CalculaOsQuatroIndicadores()
    {
        var perfil = new PerfilFinanceiro
        {
            RendaMensal = 3000, DespesasFixas = 1500, DespesasVariaveis = 500, OrcamentoRecompensa = 100,
        };
        var d = FormulasFinancas.Diagnostico(perfil, economias: 4000, aportesDoMes: 600);

        Assert.Equal(2.0, d.MesesReserva);           // 4000 ÷ 2000
        Assert.Equal(12000, d.ReservaMeta);          // 6 × 2000
        Assert.Equal(8000, d.ReservaFaltante);
        Assert.Equal(20, d.TaxaPoupancaPct);         // 600 ÷ 3000
        Assert.Equal(50, d.PctNecessidades);         // fixas ÷ renda
        Assert.Equal(30, d.PctDesejos);              // o resto
        Assert.Equal(67, d.Score);                   // 50 (poupança) + 16,7 (reserva 2/6)
        Assert.Equal("A", d.Nivel);
    }
}

public class FinancasTests : JogoServiceTestBase
{
    private Task<List<EventoDto>> DefinirPerfil(decimal renda = 3000, decimal orcamento = 100) =>
        Jogo.DefinirPerfilFinanceiro(P, new PerfilFinanceiroReq(renda, 1500, 500, orcamento));

    [Fact]
    public async Task Aporte_SomaNasEconomiasERegistra()
    {
        await Jogo.RegistrarAporte(P, 250);
        await Db.SaveChangesAsync();

        Assert.Equal(250, P.Economias);
        Assert.Equal(250, (await Db.Aportes.SingleAsync()).Valor);
    }

    [Fact]
    public async Task Retirada_NaoDeixaEconomiasNegativas()
    {
        await Jogo.RegistrarAporte(P, 100);
        await Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.RegistrarAporte(P, -200));
    }

    [Fact] // missão mensal (PRD 4.1): +300 XP +30 🪙 uma única vez por mês
    public async Task MetaDePoupanca_PagaUmaVezPorMes()
    {
        await DefinirPerfil(renda: 3000); // meta = 20% = 600
        await Db.SaveChangesAsync();
        var xpAntes = P.XpTotal;
        var moedasAntes = P.Moedas;

        var eventos = await Jogo.RegistrarAporte(P, 600);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "metaPoupanca");
        Assert.Equal(xpAntes + 300, P.XpTotal);
        Assert.Equal(moedasAntes + 30, P.Moedas);

        // mais um aporte no mesmo mês não paga de novo
        eventos = await Jogo.RegistrarAporte(P, 600);
        await Db.SaveChangesAsync();
        Assert.DoesNotContain(eventos, e => e.Tipo == "metaPoupanca");
        Assert.Equal(xpAntes + 300, P.XpTotal);

        // virar o mês reabilita a missão
        Relogio.AvancarDias(31);
        eventos = await Jogo.RegistrarAporte(P, 600);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "metaPoupanca");
        Assert.Equal(xpAntes + 600, P.XpTotal);
    }

    [Fact] // conversão exige orçamento de recompensa definido (PRD 3.8)
    public async Task Converter_SemOrcamentoFalha()
    {
        P.Moedas = 500;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.ConverterMoedas(P, 100));
    }

    [Fact] // 10 🪙 = £1, teto mensal = orçamento; extrato tipo "conversao" (PRD 3.8)
    public async Task Converter_DebitaMoedasERespeitaOTeto()
    {
        await DefinirPerfil(orcamento: 100);
        P.Moedas = 2000;
        await Db.SaveChangesAsync();

        var eventos = await Jogo.ConverterMoedas(P, 600); // £60
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "conversao");
        Assert.Equal(1400, P.Moedas);
        Assert.Equal(60, P.SaldoRecompensa);   // converter abastece o Saldo de Recompensa
        Assert.Equal("conversao", (await Db.TransacoesMoedas.SingleAsync(t => t.Tipo == "conversao")).Origem);

        // £60 + £50 passariam do orçamento de £100
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.ConverterMoedas(P, 500));

        // £40 fecham exatamente o teto
        await Jogo.ConverterMoedas(P, 400);
        await Db.SaveChangesAsync();
        Assert.Equal(1000, P.Moedas);
        Assert.Equal(100, P.SaldoRecompensa);

        var financas = await Jogo.MontarFinancas(P);
        Assert.Equal(100, financas.Conversao.ConvertidoMesLibras);
        Assert.Equal(100, financas.Conversao.SaldoRecompensa);
    }

    [Fact] // dívida quitada vira "chefe" derrotado (PRD 4.1) — sem contar em ChefesDerrotados
    public async Task DividaQuitada_PagaComoChefeSemContarNoContador()
    {
        await Jogo.CriarDivida(P, "Cartão de crédito", 800, 12);
        await Db.SaveChangesAsync();
        var divida = await Db.Dividas.SingleAsync();
        var xpAntes = P.XpTotal;

        var eventos = await Jogo.PagarDivida(P, divida.Id, 300);
        Assert.Empty(eventos.Where(e => e.Tipo == "dividaQuitada"));
        Assert.Equal(500, divida.ValorAtual);

        eventos = await Jogo.PagarDivida(P, divida.Id, 500);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "dividaQuitada" && e.Nome == "Cartão de crédito");
        Assert.Contains(eventos, e => e.Tipo == "conquista" && e.Nome == "Corrente quebrada");
        Assert.Equal(xpAntes + 1000, P.XpTotal);
        Assert.Equal(0, P.ChefesDerrotados);
        Assert.NotNull(divida.QuitadaEm);
    }

    [Fact] // com perfil financeiro, o atributo 💰 vira o score do Nível Financeiro
    public async Task AtributoFinancas_UsaOScoreQuandoHaPerfil()
    {
        var estado = await Jogo.MontarEstado(P);
        Assert.Equal(0, estado.Atributos.First(a => a.Id == "financas").Valor); // proxy sem dados

        await DefinirPerfil(renda: 3000);
        await Jogo.RegistrarAporte(P, 600); // 20% → 50 pts do lado da poupança
        await Db.SaveChangesAsync();

        estado = await Jogo.MontarEstado(P);
        Assert.True(estado.Atributos.First(a => a.Id == "financas").Valor >= 50);
    }

    [Fact] // conselhos da tabela 4.1: reserva, 50/30/20 e avalanche
    public async Task Conselhos_SeguemAsRegrasDaTabela()
    {
        await DefinirPerfil();
        await Jogo.CriarDivida(P, "Cartão", 1000, 12);
        await Jogo.CriarDivida(P, "Crediário", 2000, 4);
        await Db.SaveChangesAsync();

        var financas = await Jogo.MontarFinancas(P);
        Assert.Contains(financas.Conselhos, c => c.Contains("reserva cobre", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(financas.Conselhos, c => c.Contains("avalanche") && c.Contains("Cartão"));
        // avalanche: dívidas ordenadas por juros (cartão primeiro)
        Assert.Equal("Cartão", financas.Dividas[0].Nome);
    }

    [Fact] // anti-farm: só 1 quitação premiada por semana; a semana seguinte reabilita
    public async Task DividaQuitada_PremiaUmaVezPorSemana()
    {
        var xpAntes = P.XpTotal;

        await Jogo.CriarDivida(P, "Dívida 1", 1, 5);
        await Db.SaveChangesAsync();
        var d1 = await Db.Dividas.SingleAsync(d => d.Nome == "Dívida 1");
        await Jogo.PagarDivida(P, d1.Id, 1);
        await Db.SaveChangesAsync();
        Assert.Equal(xpAntes + 1000, P.XpTotal);

        // segunda quitação na mesma semana: quita, mas não paga XP (evita loop de dívida de £1)
        await Jogo.CriarDivida(P, "Dívida 2", 1, 5);
        await Db.SaveChangesAsync();
        var d2 = await Db.Dividas.SingleAsync(d => d.Nome == "Dívida 2");
        var eventos = await Jogo.PagarDivida(P, d2.Id, 1);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "dividaQuitada");
        Assert.Equal(xpAntes + 1000, P.XpTotal);   // sem novo prêmio
        Assert.NotNull(d2.QuitadaEm);               // mas a dívida está quitada de fato

        // virar a semana reabilita o prêmio
        Relogio.AvancarDias(7);
        await Jogo.CriarDivida(P, "Dívida 3", 1, 5);
        await Db.SaveChangesAsync();
        var d3 = await Db.Dividas.SingleAsync(d => d.Nome == "Dívida 3");
        await Jogo.PagarDivida(P, d3.Id, 1);
        await Db.SaveChangesAsync();
        Assert.Equal(xpAntes + 2000, P.XpTotal);
    }

    [Fact] // anti-farm: taxa de poupança usa o líquido (aportes − retiradas), não só os positivos
    public async Task TaxaPoupanca_UsaOLiquidoDoMes()
    {
        await DefinirPerfil(renda: 3000); // meta = 20% = 600
        await Jogo.RegistrarAporte(P, 600);
        await Db.SaveChangesAsync();

        // deposita e retira em ciclo não infla a poupança: £600 − £600 = £0 líquido
        await Jogo.RegistrarAporte(P, -600);
        await Db.SaveChangesAsync();

        var financas = await Jogo.MontarFinancas(P);
        Assert.Equal(0, financas.AportesDoMes);
        Assert.Equal(0, financas.Diagnostico!.TaxaPoupancaPct);
    }

    [Fact] // Saldo de Recompensa: converter abastece, gastar consome; não fica negativo (PRD 3.8)
    public async Task SaldoRecompensa_ConverteAbasteceEGastarConsome()
    {
        await DefinirPerfil(orcamento: 100);
        P.Moedas = 1000;
        await Db.SaveChangesAsync();

        await Jogo.ConverterMoedas(P, 500); // £50 no saldo
        await Db.SaveChangesAsync();
        Assert.Equal(50, P.SaldoRecompensa);

        var eventos = await Jogo.RegistrarGastoRecompensa(P, 30, "Jantar fora");
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "gastoRecompensa" && e.Nome == "Jantar fora");
        Assert.Equal(20, P.SaldoRecompensa);
        Assert.Equal("gastoRecompensa", (await Db.TransacoesMoedas.SingleAsync(t => t.Tipo == "gastoRecompensa")).Tipo);

        // gastar mais que o saldo falha, sem deixar negativo
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.RegistrarGastoRecompensa(P, 50, "Wishlist"));
        Assert.Equal(20, P.SaldoRecompensa);
    }

    [Fact] // teto mensal da conversão usa o fuso do jogador, não UTC (bug na virada do mês)
    public async Task ConversaoTetoMensal_UsaOFusoDoJogador()
    {
        await DefinirPerfil(orcamento: 10);
        P.Moedas = 500;
        await Db.SaveChangesAsync();

        // 01/08 às 01:00 UTC = 31/07 às 22:00 no fuso BRT (UTC-3): ainda é JULHO para o jogador
        Relogio.AgoraUtc = new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc);
        await Jogo.ConverterMoedas(P, 100); // £10 — fecha o teto de julho
        await Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.ConverterMoedas(P, 10)); // julho cheio

        // avança 3h → 01/08 às 04:00 UTC = 01/08 às 01:00 BRT: agora é AGOSTO, teto reabre
        Relogio.AgoraUtc = new DateTime(2026, 8, 1, 4, 0, 0, DateTimeKind.Utc);
        await Jogo.ConverterMoedas(P, 100); // £10 em agosto, ok
        await Db.SaveChangesAsync();
        Assert.Equal(300, P.Moedas);
        Assert.Equal(20, P.SaldoRecompensa);
    }

    [Fact] // teto anti-inflação: o cofre para em TetoMoedas e o ganho excedente não entra
    public async Task Moedas_ParamNoTetoAntiInflacao()
    {
        P.Moedas = Services.JogoService.TetoMoedas - 10;
        await Db.SaveChangesAsync();

        // uma conquista paga +100 🪙, mas só 10 cabem até o teto
        await Jogo.DefinirPerfilFinanceiro(P, new Contracts.PerfilFinanceiroReq(3000, 1500, 500, 100));
        await Jogo.CriarDivida(P, "Cartão", 100, 12);
        await Db.SaveChangesAsync();
        var divida = await Db.Dividas.SingleAsync();
        await Jogo.PagarDivida(P, divida.Id, 100); // +200 🪙 (dívida) + 100 🪙 (conquista dividaZero)
        await Db.SaveChangesAsync();

        Assert.Equal(Services.JogoService.TetoMoedas, P.Moedas); // travado no teto, não passou
    }
}

public class MenteTests : JogoServiceTestBase
{
    [Fact]
    public async Task Trilha_NaoPodeSerSeguidaDuasVezes()
    {
        await Jogo.CriarHabilidade(P, "csharp", null);
        await Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.CriarHabilidade(P, "csharp", null));
    }

    [Fact] // marco concluído: +50 XP +5 🪙, idempotente
    public async Task ConcluirMarco_PagaUmaVez()
    {
        await Jogo.CriarHabilidade(P, "csharp", null);
        await Db.SaveChangesAsync();
        var habilidade = await Db.Habilidades.SingleAsync();
        var xpAntes = P.XpTotal;

        var eventos = await Jogo.ConcluirMarco(P, habilidade.Id, 0);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "marco");
        Assert.Equal(xpAntes + 50, P.XpTotal);

        eventos = await Jogo.ConcluirMarco(P, habilidade.Id, 0);
        Assert.Empty(eventos);
        Assert.Equal(xpAntes + 50, P.XpTotal);
    }

    [Fact] // habilidade personalizada não tem marco manual — destrava por horas de foco
    public async Task MarcoManual_SoNasTrilhasDoCatalogo()
    {
        await Jogo.CriarHabilidade(P, null, "Xadrez");
        await Db.SaveChangesAsync();
        var habilidade = await Db.Habilidades.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.ConcluirMarco(P, habilidade.Id, 0));
    }

    [Fact] // o tempo do Modo Foco é creditado à habilidade escolhida (PRD 4.4)
    public async Task Foco_CreditaHorasNaHabilidade()
    {
        await Jogo.CriarHabilidade(P, null, "Xadrez");
        await Db.SaveChangesAsync();
        var habilidade = await Db.Habilidades.SingleAsync();

        // 12 ciclos de 50 min = 10h → primeiro marco por horas
        for (var i = 0; i < 12; i++)
        {
            await Jogo.IniciarFoco(P, "foco", habilidade.Id);
            await Db.SaveChangesAsync();
            Relogio.AvancarMinutos(50);
            await Jogo.EncerrarFoco(P, abandonar: false);
            await Db.SaveChangesAsync();
            Relogio.AvancarMinutos(10);
        }

        var mente = await Jogo.MontarMente(P);
        var dto = mente.Habilidades.Single();
        Assert.Equal(10, dto.HorasFoco);
        Assert.True(dto.Marcos[0].Concluido);   // 10h
        Assert.False(dto.Marcos[1].Concluido);  // 25h
        Assert.Equal(1, mente.MarcosConcluidos);
    }

    [Fact] // Conhecimento (PRD 3.1): livros + marcos → 4 pts cada
    public async Task Conhecimento_UsaLivrosEMarcos()
    {
        await Jogo.CriarHabilidade(P, "react", null);
        await Jogo.CriarLivro(P, "Clean Code", null);
        await Db.SaveChangesAsync();
        var habilidade = await Db.Habilidades.SingleAsync();
        var livro = await Db.Livros.SingleAsync();

        var eventos = await Jogo.ConcluirLivro(P, livro.Id);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "livroConcluido");
        Assert.Contains(eventos, e => e.Tipo == "conquista" && e.Nome == "Primeira página virada");

        await Jogo.ConcluirMarco(P, habilidade.Id, 0);
        await Db.SaveChangesAsync();

        var estado = await Jogo.MontarEstado(P);
        Assert.Equal(8, estado.Atributos.First(a => a.Id == "conhecimento").Valor); // 1 livro + 1 marco
    }

    [Fact] // Carisma: consistência de interações em 30 dias; 1 por dia
    public async Task InteracaoSocial_AlimentaOCarisma()
    {
        await Jogo.RegistrarInteracaoSocial(P);
        await Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.RegistrarInteracaoSocial(P));

        for (var i = 0; i < 14; i++)
        {
            Relogio.AvancarDias(1);
            await Jogo.RegistrarInteracaoSocial(P);
            await Db.SaveChangesAsync();
        }

        var estado = await Jogo.MontarEstado(P);
        Assert.Equal(50, estado.Atributos.First(a => a.Id == "carisma").Valor); // 15/30 dias
    }

    [Fact] // anti-farm: só 1 livro premiado por semana, mas todo livro conta para o Conhecimento
    public async Task Livro_PremiaUmaVezPorSemanaMasSempreContaNoConhecimento()
    {
        var xpAntes = P.XpTotal;
        await Jogo.CriarLivro(P, "Livro 1", null);
        await Jogo.CriarLivro(P, "Livro 2", null);
        await Db.SaveChangesAsync();
        var ids = await Db.Livros.OrderBy(l => l.Id).Select(l => l.Id).ToListAsync();

        await Jogo.ConcluirLivro(P, ids[0]);
        await Db.SaveChangesAsync();
        Assert.Equal(xpAntes + 100, P.XpTotal);

        // segundo livro na mesma semana: conclui e conta, mas não paga XP
        var eventos = await Jogo.ConcluirLivro(P, ids[1]);
        await Db.SaveChangesAsync();
        Assert.Contains(eventos, e => e.Tipo == "livroConcluido");
        Assert.Equal(xpAntes + 100, P.XpTotal); // sem novo prêmio

        // ambos alimentam o Conhecimento (2 livros × 4 pts = 8)
        var estado = await Jogo.MontarEstado(P);
        Assert.Equal(8, estado.Atributos.First(a => a.Id == "conhecimento").Valor);

        // virar a semana reabilita o prêmio
        Relogio.AvancarDias(7);
        await Jogo.CriarLivro(P, "Livro 3", null);
        await Db.SaveChangesAsync();
        var id3 = await Db.Livros.OrderByDescending(l => l.Id).Select(l => l.Id).FirstAsync();
        await Jogo.ConcluirLivro(P, id3);
        await Db.SaveChangesAsync();
        Assert.Equal(xpAntes + 200, P.XpTotal);
    }
}
