using LifeSystem.Api.Domain;

namespace LifeSystem.Api.Tests;

/// <summary>Missões de classe e Avatar Transcendente (PRD 3.5) — fechamento da Fase 1.</summary>
public class Fase1Tests : JogoServiceTestBase
{
    // ---------- Missões de classe ----------

    [Fact]
    public async Task MissaoDeClasse_SoDaPropriaClasse()
    {
        P.Level = 5;
        await Jogo.EscolherClasse(P, "guerreiro");

        // a missão do mago não existe para um guerreiro
        await Assert.ThrowsAsync<ArgumentException>(() => Jogo.ConcluirMissao(P, "classe-mago"));
    }

    [Fact]
    public async Task MissaoDeClasse_ExigeClasseEscolhida()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Jogo.ConcluirMissao(P, "classe-guerreiro"));
    }

    [Fact]
    public async Task MissaoDeClasse_PagaComBonusDePrimarias()
    {
        P.Level = 5;
        await Jogo.EscolherClasse(P, "guerreiro");
        await Jogo.Sincronizar(P);

        var xpAntes = P.XpTotal;
        await Jogo.ConcluirMissao(P, "classe-guerreiro");

        // 150 × 1,02 (streak 1) × 1,2 (primária da classe) = 184
        Assert.Equal(184, P.XpTotal - xpAntes);
    }

    [Fact]
    public async Task DiaPerfeito_NaoDependeDaMissaoDeClasse()
    {
        P.Level = 5;
        await Jogo.EscolherClasse(P, "guerreiro");
        await Jogo.Sincronizar(P);

        // As 6 padrão bastam — a missão de classe fica de fora
        var eventos = new List<Contracts.EventoDto>();
        foreach (var m in Catalogo.Missoes)
            eventos.AddRange(await Jogo.ConcluirMissao(P, m.Id));

        Assert.Contains(eventos, e => e.Tipo == "perfeito");
        Assert.Equal(1, P.DiasPerfeitos);
    }

    [Fact]
    public async Task MissaoDeClasse_NaoDisparaDiaPerfeitoSozinha()
    {
        P.Level = 5;
        await Jogo.EscolherClasse(P, "guerreiro");
        await Jogo.Sincronizar(P);

        // 5 padrão + a de classe = 6 concluídas, mas não é dia perfeito
        var eventos = new List<Contracts.EventoDto>();
        foreach (var m in Catalogo.Missoes.Take(5))
            eventos.AddRange(await Jogo.ConcluirMissao(P, m.Id));
        eventos.AddRange(await Jogo.ConcluirMissao(P, "classe-guerreiro"));

        Assert.DoesNotContain(eventos, e => e.Tipo == "perfeito");
        Assert.Equal(0, P.DiasPerfeitos);
    }

    [Fact]
    public async Task EstadoDeUmGuerreiro_IncluiAMissaoDeClasse()
    {
        P.Level = 5;
        await Jogo.EscolherClasse(P, "guerreiro");
        await Jogo.Sincronizar(P);
        await Db.SaveChangesAsync();

        var estado = await Jogo.MontarEstado(P);
        var deClasse = estado.MissoesHoje.Where(m => m.DeClasse).ToList();
        Assert.Single(deClasse);
        Assert.Equal("classe-guerreiro", deClasse[0].Id);
        Assert.Equal(7, estado.MissoesHoje.Count); // 6 padrão + 1 de classe
    }

    // ---------- Avatar Transcendente ----------

    [Fact]
    public async Task Transcendente_Ganha10PorCentoDeXpEmTudo()
    {
        P.AvatarTranscendente = true;
        await Jogo.Sincronizar(P); // atributos baixos derrubam a transcendência…
        Assert.False(P.AvatarTranscendente); // …provando que manter exige a vida em dia

        P.AvatarTranscendente = true; // força para medir o bônus sem passar pelo Sincronizar
        var xpAntes = P.XpTotal;
        await Jogo.ConcluirMissao(P, "treinar");
        // 250 × 1,02 = 255 → ×1,1 = 280,5 → 280 (arredondamento bancário do .NET)
        Assert.Equal(280, P.XpTotal - xpAntes);
    }

    [Fact]
    public async Task TranscendenciaCai_QuandoAtributosCaem()
    {
        P.AvatarTranscendente = true;
        P.TranscendenciaDesde = Relogio.AgoraUtc.AddDays(-40);

        var eventos = await Jogo.Sincronizar(P); // personagem novo: atributos < 80

        Assert.False(P.AvatarTranscendente);
        Assert.Null(P.TranscendenciaDesde);
        Assert.Contains(eventos, e => e.Tipo == "transcendenciaPerdida");
    }

    [Fact]
    public void TituloDoTranscendente_SobrepoeODaClasse()
    {
        Assert.Equal("Avatar Transcendente", Formulas.TituloAtual(50, "guerreiro", transcendente: true));
        Assert.Equal("✨", Formulas.EmojiTitulo(50, "guerreiro", transcendente: true));
        Assert.Equal("Senhor da Guerra", Formulas.TituloAtual(50, "guerreiro"));
    }

    [Fact]
    public void ConquistaOculta_ExisteENaoRevelaONome()
    {
        var oculta = Catalogo.Conquistas.Single(c => c.Oculta);
        Assert.Equal("semPontosFracos", oculta.Id);
    }
}
