using LifeSystem.Api.Contracts;
using LifeSystem.Api.Domain;
using LifeSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Tests;

public class FormulasCorpoTests
{
    [Theory] // Epley: carga × (1 + reps/30); 1 rep = a própria carga (PRD 4.3)
    [InlineData(100, 1, 100)]
    [InlineData(100, 10, 133.3)]
    [InlineData(60, 30, 120)]
    public void Rm1Epley_SegueAFormula(double carga, int reps, double esperado) =>
        Assert.Equal(esperado, FormulasCorpo.Rm1Epley(carga, reps), precision: 1);

    [Theory] // pace em segundos por km
    [InlineData(5, 25, 300)]
    [InlineData(10, 60, 360)]
    public void Pace_EmSegundosPorKm(double km, double min, int esperado) =>
        Assert.Equal(esperado, FormulasCorpo.PaceSegPorKm(km, min));

    [Theory] // a corrida entra na maior faixa que cobre (PRD 4.3: 7 km → faixa de 5 km)
    [InlineData(0.8, null)]
    [InlineData(1, 1)]
    [InlineData(7, 5)]
    [InlineData(10, 10)]
    [InlineData(21.1, 21)]
    [InlineData(42.2, 42)]
    public void FaixaDeCardio_MaiorDistanciaCoberta(double km, int? esperada) =>
        Assert.Equal(esperada, FormulasCorpo.FaixaDe(km));

    [Theory] // âncoras do PRD 3.1: 1× peso ≈ 50 · 1,5× ≈ 70 · 2× ≈ 90
    [InlineData(0, 0)]
    [InlineData(0.5, 25)]
    [InlineData(1.0, 50)]
    [InlineData(1.5, 70)]
    [InlineData(2.0, 90)]
    [InlineData(3.0, 100)]
    public void ForcaReal_SegueAsAncorasDoPrd(double rel, int esperado) =>
        Assert.Equal(esperado, FormulasCorpo.ForcaDe(rel));

    [Theory] // âncoras do PRD 3.1: 5k em 30min ≈ 40 · 25min ≈ 60 · 20min ≈ 85 (sem volume)
    [InlineData(360, 0, 40)]  // 6:00/km → 5k em 30min
    [InlineData(300, 0, 60)]  // 5:00/km → 25min
    [InlineData(240, 0, 85)]  // 4:00/km → 20min
    [InlineData(240, 100, 95)] // volume soma até +15 (100 km/mês = +10)
    public void ResistenciaReal_SegueAsAncorasDoPrd(int paceSeg, double km30, int esperado) =>
        Assert.Equal(esperado, FormulasCorpo.ResistenciaDe(paceSeg, km30));

    [Fact] // Mifflin-St Jeor: homem 80 kg, 180 cm, 30 anos → TMB 1780; moderado ×1,55; manter
    public void Metas_MifflinStJeor()
    {
        var m = FormulasCorpo.Metas(80, 180, 30, "m", "moderado", "manter");
        Assert.Equal(2759, m.Calorias);       // 1780 × 1,55
        Assert.Equal(144, m.ProteinaG);       // 1,8 g/kg
        Assert.Equal(72, m.GorduraG);         // 0,9 g/kg
        Assert.Equal(30, m.FibrasG);
        Assert.Equal(2800, m.AguaMl);         // 35 ml/kg
        Assert.True(m.CarboG > 0);
    }

    [Fact] // déficit de 20% no emagrecimento, proteína sobe para 2,2 g/kg
    public void Metas_EmagrecerTemDeficit()
    {
        var manter = FormulasCorpo.Metas(80, 180, 30, "m", "moderado", "manter");
        var emagrecer = FormulasCorpo.Metas(80, 180, 30, "m", "moderado", "emagrecer");
        Assert.Equal((int)Math.Round(manter.Calorias * 0.8), emagrecer.Calorias);
        Assert.Equal(176, emagrecer.ProteinaG); // 2,2 g/kg
    }

    [Fact] // plano de refeições cobre ~100% das calorias
    public void PlanoDeRefeicoes_SomaAsCalorias()
    {
        var m = FormulasCorpo.Metas(80, 180, 30, "m", "moderado", "manter");
        var plano = FormulasCorpo.PlanoRefeicoes(m);
        Assert.Equal(5, plano.Count);
        Assert.InRange(plano.Sum(r => r.Kcal), m.Calorias - 10, m.Calorias + 10);
    }
}

public class CorpoTests : JogoServiceTestBase
{
    private Task<List<EventoDto>> DefinirPerfil(double peso = 80) =>
        Jogo.DefinirPerfilCorporal(P, new PerfilReq(peso, 180, 30, "m", "moderado", "manter"));

    [Fact]
    public async Task RegistrarCarga_PrimeiroRegistroEhPrEDaPremio()
    {
        var moedasAntes = P.Moedas;
        var eventos = await Jogo.RegistrarCarga(P, "supino", 80, 5);
        await Db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "novoRecorde");
        Assert.Contains(eventos, e => e.Tipo == "conquista" && e.Nome == "Primeiro PR");
        var registro = await Db.RegistrosCarga.SingleAsync();
        Assert.True(registro.Pr);
        Assert.True(registro.PrPremiado);
        Assert.Equal(93.3, registro.Rm1, precision: 1);
        Assert.True(P.Moedas > moedasAntes);
        // registrar treino completa a missão do dia (PRD 3.3)
        Assert.True(await Db.MissoesLog.AnyAsync(m => m.MissaoId == "treinar" && m.Concluida));
    }

    [Fact]
    public async Task RegistrarCarga_SoPremiaUmPrPorExercicioPorSemana()
    {
        await Jogo.RegistrarCarga(P, "supino", 80, 5);
        await Db.SaveChangesAsync();
        var xpAntes = P.XpTotal;

        var eventos = await Jogo.RegistrarCarga(P, "supino", 85, 5); // PR de novo, mesma semana
        await Db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "novoRecorde"); // celebra
        Assert.Equal(2, await Db.RegistrosCarga.CountAsync(r => r.Pr));
        Assert.Equal(1, await Db.RegistrosCarga.CountAsync(r => r.PrPremiado)); // mas não paga
        Assert.Equal(xpAntes, P.XpTotal);

        Relogio.AvancarDias(7); // semana nova → prêmio de novo
        await Jogo.RegistrarCarga(P, "supino", 90, 5);
        await Db.SaveChangesAsync();
        Assert.Equal(2, await Db.RegistrosCarga.CountAsync(r => r.PrPremiado));
    }

    [Fact]
    public async Task RegistrarCarga_CargaMenorNaoEhPr()
    {
        await Jogo.RegistrarCarga(P, "supino", 80, 5);
        await Db.SaveChangesAsync();
        var eventos = await Jogo.RegistrarCarga(P, "supino", 60, 5);
        Assert.DoesNotContain(eventos, e => e.Tipo == "novoRecorde");
    }

    [Fact]
    public async Task ForcaReal_SubstituiOProxyQuandoHaDadosEPerfil()
    {
        await DefinirPerfil(peso: 80);
        // 1RM 120 nos três básicos → relativo 1,5 → Força 70 (âncora do PRD)
        foreach (var ex in new[] { "supino", "agachamento", "terra" })
            await Jogo.RegistrarCarga(P, ex, 120, 1);
        await Db.SaveChangesAsync();

        var estado = await Jogo.MontarEstado(P);
        Assert.Equal(70, estado.Atributos.First(a => a.Id == "forca").Valor);
    }

    [Fact]
    public async Task ResistenciaReal_UsaPace5kEVolume()
    {
        // 5 km em 25 min → pace 300 s/km → 60 + volume 5/10 = 60,5 → 60 (arredondamento bancário)
        await Jogo.RegistrarCardio(P, 5, 25);
        await Db.SaveChangesAsync();
        var estado = await Jogo.MontarEstado(P);
        Assert.Equal(60, estado.Atributos.First(a => a.Id == "resistencia").Valor);
    }

    [Fact]
    public async Task RegistrarCardio_PrPorFaixaEConquistas()
    {
        var eventos = await Jogo.RegistrarCardio(P, 5.2, 30);
        await Db.SaveChangesAsync();

        Assert.Contains(eventos, e => e.Tipo == "novoRecorde");
        Assert.Contains(eventos, e => e.Tipo == "conquista" && e.Nome == "Primeiros 5 km");
        var registro = await Db.RegistrosCardio.SingleAsync();
        Assert.Equal(5, registro.FaixaKm);
        Assert.Equal(346, registro.PaceSegKm);

        // pace pior na mesma faixa não é PR
        var eventos2 = await Jogo.RegistrarCardio(P, 5, 35);
        Assert.DoesNotContain(eventos2, e => e.Tipo == "novoRecorde");
    }

    [Fact]
    public async Task ChecklistDaAlimentacao_UsaAsMetasDoPerfil()
    {
        await DefinirPerfil(peso: 80);
        await Db.SaveChangesAsync();
        var estado = await Jogo.MontarEstado(P);
        var alimentacao = estado.MissoesHoje.First(m => m.Id == "alimentacao");
        Assert.Equal("144g de proteína", alimentacao.Checklist![0]); // 1,8 g/kg × 80
        Assert.Equal("2,8L de água", alimentacao.Checklist![3]);     // 35 ml/kg × 80
        Assert.Equal(4, alimentacao.Checks.Length);                  // mesmo tamanho: os checks continuam válidos
    }

    [Fact]
    public async Task PerfilInvalido_EhRejeitado()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Jogo.DefinirPerfilCorporal(P, new PerfilReq(20, 180, 30, "m", "moderado", "manter")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Jogo.DefinirPerfilCorporal(P, new PerfilReq(80, 180, 30, "x", "moderado", "manter")));
    }

    [Fact]
    public async Task MontarCorpo_GeraCodigoDeAmigoEMetas()
    {
        await DefinirPerfil();
        await Db.SaveChangesAsync();
        var corpo = await Jogo.MontarCorpo(P);

        Assert.Equal(6, corpo.CodigoAmigo.Length);
        Assert.NotNull(corpo.Metas);
        Assert.Equal(2759, corpo.Metas!.Calorias);
        Assert.Equal(5, corpo.Plano.Count);
        Assert.False(corpo.RankingOptIn); // opt-in começa desligado (PRD 4.3)
    }

    private async Task<Personagem> CriarSegundoJogador(string nome)
    {
        var usuario = new Usuario { Email = $"{nome}@teste.com", SenhaHash = "x" };
        Db.Usuarios.Add(usuario);
        await Db.SaveChangesAsync();
        var p2 = JogoService.NovoPersonagem(usuario.Id, nome);
        Db.Personagens.Add(p2);
        await Db.SaveChangesAsync();
        return p2;
    }

    [Fact]
    public async Task Amizade_ConviteAceiteERanking()
    {
        var p2 = await CriarSegundoJogador("Rival");
        await Jogo.MontarCorpo(p2); // gera o código do rival
        await Db.SaveChangesAsync();

        await Jogo.AdicionarAmigo(P, p2.CodigoAmigo!);
        await Db.SaveChangesAsync();

        // o rival vê o convite pendente e aceita
        var corpoRival = await Jogo.MontarCorpo(p2);
        var convite = Assert.Single(corpoRival.Amigos);
        Assert.Equal("pendenteRecebido", convite.Situacao);
        await Jogo.ResponderAmizade(p2, convite.AmizadeId, aceitar: true);
        await Db.SaveChangesAsync();

        // os dois registram supino → ranking de amigos tem os dois; geral só com opt-in
        await Jogo.RegistrarCarga(P, "supino", 100, 1);
        await Jogo.RegistrarCarga(p2, "supino", 90, 1);
        await Db.SaveChangesAsync();

        var corpo = await Jogo.MontarCorpo(P);
        var ranking = corpo.RankingsForca.First(r => r.Chave == "supino");
        Assert.Equal(2, ranking.Amigos.Count);
        Assert.Empty(ranking.Geral); // ninguém deu opt-in

        await Jogo.DefinirRankingOptIn(P, true);
        await Db.SaveChangesAsync();
        corpo = await Jogo.MontarCorpo(P);
        Assert.Single(corpo.RankingsForca.First(r => r.Chave == "supino").Geral);
    }

    [Fact]
    public async Task Amizade_CodigoProprioEDuplicadoSaoRejeitados()
    {
        var corpo = await Jogo.MontarCorpo(P);
        await Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => Jogo.AdicionarAmigo(P, corpo.CodigoAmigo));

        var p2 = await CriarSegundoJogador("Rival");
        await Jogo.MontarCorpo(p2);
        await Db.SaveChangesAsync();
        await Jogo.AdicionarAmigo(P, p2.CodigoAmigo!);
        await Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Jogo.AdicionarAmigo(P, p2.CodigoAmigo!));
    }

    [Fact]
    public async Task ConquistasDeCorrida_10kAbaixoDe60EMeia()
    {
        var eventos = await Jogo.RegistrarCardio(P, 10, 55);
        Assert.Contains(eventos, e => e.Tipo == "conquista" && e.Nome == "10 km abaixo de 60 min");
        await Db.SaveChangesAsync();

        Relogio.AvancarDias(1);
        var eventos2 = await Jogo.RegistrarCardio(P, 21.1, 130);
        Assert.Contains(eventos2, e => e.Tipo == "conquista" && e.Nome == "Primeira meia-maratona");
    }

    [Fact]
    public async Task AvisoDeSaude_FicaRegistrado()
    {
        Assert.Null(P.AvisoSaudeAceitoEm);
        await Jogo.AceitarAvisoSaude(P);
        Assert.NotNull(P.AvisoSaudeAceitoEm);
        var corpo = await Jogo.MontarCorpo(P);
        Assert.True(corpo.AvisoSaudeAceito);
    }

    [Fact]
    public async Task ConselhoDeProgressao_DetectaCargaEstagnada()
    {
        await DefinirPerfil();
        for (var i = 0; i < 3; i++)
        {
            await Jogo.RegistrarCarga(P, "supino", 80, 8);
            await Db.SaveChangesAsync();
            Relogio.AvancarDias(3);
        }
        var corpo = await Jogo.MontarCorpo(P);
        Assert.Contains(corpo.Conselhos, c => c.Contains("Supino reto") && c.Contains("+2,5 kg"));
    }
}
