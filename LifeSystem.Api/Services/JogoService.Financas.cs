using LifeSystem.Api.Contracts;
using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Services;

/// <summary>
/// Fase 3 — Bolso (PRD 4.1 e 3.8): diagnóstico de saúde financeira, dívidas (avalanche),
/// missão mensal de poupança e conversão de moedas no orçamento de recompensa.
/// </summary>
public partial class JogoService
{
    private const int MetaPoupancaXp = 300;      // missão mensal (PRD 4.1)
    private const int MetaPoupancaMoedas = 30;
    private const int DividaQuitadaXp = 1000;    // "chefe" derrotado (PRD 4.1) — mesmas recompensas do chefe semanal
    private const int DividaQuitadaMoedas = 200;

    // ---------- Perfil e aviso ----------

    public Task<List<EventoDto>> DefinirPerfilFinanceiro(Personagem p, PerfilFinanceiroReq req)
    {
        if (req.RendaMensal is < 0 or > 1_000_000) throw new ArgumentException("Renda fora da faixa");
        if (req.DespesasFixas < 0 || req.DespesasVariaveis < 0) throw new ArgumentException("Despesas não podem ser negativas");
        if (req.DespesasFixas + req.DespesasVariaveis > req.RendaMensal * 3)
            throw new ArgumentException("Despesas muito acima da renda — confira os valores");
        if (req.OrcamentoRecompensa is < 0 or > 100_000) throw new ArgumentException("Orçamento de recompensa fora da faixa");

        var perfil = p.PerfilFinanceiro;
        if (perfil is null)
        {
            perfil = new PerfilFinanceiro { PersonagemId = p.Id };
            p.PerfilFinanceiro = perfil;
            db.PerfisFinanceiros.Add(perfil);
        }
        perfil.RendaMensal = req.RendaMensal;
        perfil.DespesasFixas = req.DespesasFixas;
        perfil.DespesasVariaveis = req.DespesasVariaveis;
        perfil.OrcamentoRecompensa = req.OrcamentoRecompensa;
        perfil.AtualizadoEm = relogio.AgoraUtc;
        return Task.FromResult(new List<EventoDto>());
    }

    /// <summary>Disclaimer obrigatório (PRD 4.5): aceite no primeiro uso de cada módulo.</summary>
    public Task<List<EventoDto>> AceitarAvisoFinancas(Personagem p)
    {
        p.AvisoFinancasAceitoEm ??= relogio.AgoraUtc;
        return Task.FromResult(new List<EventoDto>());
    }

    // ---------- Aportes e missão mensal de poupança ----------

    public async Task<List<EventoDto>> RegistrarAporte(Personagem p, decimal valor)
    {
        if (valor == 0 || Math.Abs(valor) > 1_000_000) throw new ArgumentException("Valor de aporte inválido");
        if (valor < 0 && p.Economias + valor < 0) throw new InvalidOperationException("Retirada maior que as economias");

        var hoje = relogio.Hoje;
        db.Aportes.Add(new AporteEconomia
        {
            PersonagemId = p.Id, Valor = valor, Data = hoje, CriadoEm = relogio.AgoraUtc,
        });
        p.Economias += valor;

        var eventos = new List<EventoDto>();

        // Missão mensal (PRD 4.1): bater a meta de poupança do mês = +300 XP, +30 🪙 — uma vez por mês
        var perfil = p.PerfilFinanceiro;
        if (perfil is not null && perfil.RendaMensal > 0)
        {
            var meta = perfil.RendaMensal * (decimal)(FormulasFinancas.MetaPoupancaPct / 100);
            var aportesDoMes = await AportesDoMes(p, hoje);
            var origem = $"metaPoupanca:{hoje:yyyy-MM}";
            var jaPremiado = await db.TransacoesMoedas
                .AnyAsync(t => t.PersonagemId == p.Id && t.Origem == origem);
            if (aportesDoMes >= meta && !jaPremiado)
            {
                GanharMoedas(p, MetaPoupancaMoedas, origem);
                AplicarXp(p, MetaPoupancaXp, eventos);
                eventos.Add(new("metaPoupanca"));
            }
        }

        eventos.AddRange(await ChecarConquistas(p));          // poupanca10k usa p.Economias
        eventos.AddRange(await ChecarConquistasFinancas(p));
        return eventos;
    }

    /// <summary>
    /// Poupança líquida do mês corrente: aportes − retiradas, nunca negativa (banco + esta requisição).
    /// Anti-farm: contar só os positivos permitia inflar a taxa de poupança depositando e retirando em ciclo.
    /// </summary>
    private async Task<decimal> AportesDoMes(Personagem p, DateOnly hoje)
    {
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);
        // SQLite não agrega decimal no servidor — soma no cliente (poucas linhas por mês)
        var noBanco = (await db.Aportes
            .Where(a => a.PersonagemId == p.Id && a.Data >= inicioMes)
            .Select(a => a.Valor)
            .ToListAsync()).Sum();
        var local = db.Aportes.Local
            .Where(a => a.PersonagemId == p.Id && a.Data >= inicioMes && a.Id == 0)
            .Sum(a => a.Valor);
        return Math.Max(0, noBanco + local);
    }

    // ---------- Dívidas (método avalanche, PRD 4.1) ----------

    public Task<List<EventoDto>> CriarDivida(Personagem p, string nome, decimal valor, double jurosPctMes)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Dê um nome à dívida");
        if (valor is <= 0 or > 10_000_000) throw new ArgumentException("Valor da dívida fora da faixa");
        if (jurosPctMes is < 0 or > 100) throw new ArgumentException("Juros fora da faixa (0–100%/mês)");

        db.Dividas.Add(new Divida
        {
            PersonagemId = p.Id, Nome = nome.Trim(), ValorAtual = valor,
            JurosPctMes = jurosPctMes, CriadaEm = relogio.AgoraUtc,
        });
        return Task.FromResult(new List<EventoDto>());
    }

    public async Task<List<EventoDto>> PagarDivida(Personagem p, int dividaId, decimal valor)
    {
        var divida = await db.Dividas
            .FirstOrDefaultAsync(d => d.Id == dividaId && d.PersonagemId == p.Id && d.QuitadaEm == null)
            ?? throw new ArgumentException("Dívida não encontrada");
        if (valor <= 0) throw new ArgumentException("Valor do pagamento inválido");

        divida.ValorAtual = Math.Max(0, divida.ValorAtual - valor);
        var eventos = new List<EventoDto>();
        if (divida.ValorAtual == 0)
        {
            // Dívida quitada vira um "chefe" derrotado (PRD 4.1) — sem contar em ChefesDerrotados.
            // Anti-farm: máx. 1 quitação premiada por semana (senão dívida de £1 vira loop de XP,
            // furando o teto anti-inflação do PRD 3.2).
            divida.QuitadaEm = relogio.AgoraUtc;
            var segunda = Formulas.SegundaFeiraDe(relogio.Hoje);
            var quitacoesPremiadas = await db.Dividas
                .Where(d => d.PersonagemId == p.Id && d.Premiada && d.QuitadaEm != null)
                .Select(d => d.QuitadaEm!.Value)
                .ToListAsync();
            var jaPremiadaNaSemana = quitacoesPremiadas.Any(q => relogio.DataDe(q) >= segunda);

            if (!jaPremiadaNaSemana)
            {
                divida.Premiada = true;
                GanharMoedas(p, DividaQuitadaMoedas, $"divida:{divida.Nome}");
                AplicarXp(p, DividaQuitadaXp, eventos);
                eventos.Add(new("dividaQuitada", Nome: divida.Nome, Emoji: "⛓️",
                    Titulo: $"Um chefe a menos no bolso: +{DividaQuitadaXp:#,0} XP · +{DividaQuitadaMoedas} 🪙"));
            }
            else
            {
                eventos.Add(new("dividaQuitada", Nome: divida.Nome, Emoji: "⛓️",
                    Titulo: "Corrente quebrada! (prêmio já usado esta semana — máx. 1 quitação premiada/semana)"));
            }
            eventos.AddRange(await ChecarConquistasFinancas(p));
        }
        return eventos;
    }

    // ---------- Conversão de moedas (PRD 3.8) ----------

    public async Task<List<EventoDto>> ConverterMoedas(Personagem p, int moedas)
    {
        var perfil = p.PerfilFinanceiro;
        if (perfil is null || perfil.OrcamentoRecompensa <= 0)
            throw new InvalidOperationException("Defina o orçamento de recompensa no seu perfil financeiro primeiro");
        if (moedas < FormulasFinancas.MoedasPorLibra) throw new ArgumentException($"Converta pelo menos {FormulasFinancas.MoedasPorLibra} 🪙 (= £1)");
        if (moedas % FormulasFinancas.MoedasPorLibra != 0) throw new ArgumentException($"Converta múltiplos de {FormulasFinancas.MoedasPorLibra} 🪙");
        if (p.Moedas < moedas) throw new InvalidOperationException("Moedas insuficientes");

        var libras = (decimal)moedas / FormulasFinancas.MoedasPorLibra;
        var convertidoMes = await ConvertidoNoMes(p, relogio.Hoje);
        if (convertidoMes + libras > perfil.OrcamentoRecompensa)
            throw new InvalidOperationException(
                $"Conversão passaria do orçamento do mês (£{perfil.OrcamentoRecompensa - convertidoMes:0.##} restantes)");

        p.Moedas -= moedas;
        db.TransacoesMoedas.Add(new TransacaoMoedas
        {
            PersonagemId = p.Id, Tipo = "conversao", Valor = moedas, Origem = "conversao",
            CriadoEm = relogio.AgoraUtc,
        });
        return [new("conversao", Titulo: $"£{libras:0.##} liberadas para gastar sem culpa")];
    }

    /// <summary>Libras já convertidas no mês corrente (teto = orçamento de recompensa, PRD 3.8).</summary>
    private async Task<decimal> ConvertidoNoMes(Personagem p, DateOnly hoje)
    {
        var inicioMesUtc = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var moedas = await db.TransacoesMoedas
            .Where(t => t.PersonagemId == p.Id && t.Tipo == "conversao" && t.CriadoEm >= inicioMesUtc)
            .SumAsync(t => t.Valor);
        return (decimal)moedas / FormulasFinancas.MoedasPorLibra;
    }

    // ---------- Conquistas do Bolso ----------

    private async Task<List<EventoDto>> ChecarConquistasFinancas(Personagem p)
    {
        var eventos = new List<EventoDto>();
        var desbloqueadas = p.Conquistas.Select(c => c.ConquistaId).ToHashSet();

        var quitouDivida = db.Dividas.Local.Any(d => d.PersonagemId == p.Id && d.QuitadaEm != null)
            || await db.Dividas.AnyAsync(d => d.PersonagemId == p.Id && d.QuitadaEm != null);

        var reserva6m = false;
        var nivelA = false;
        if (p.PerfilFinanceiro is { } perfil)
        {
            var diag = FormulasFinancas.Diagnostico(perfil, p.Economias, await AportesDoMes(p, relogio.Hoje));
            reserva6m = diag.MesesReserva >= FormulasFinancas.MesesReservaMeta;
            nivelA = diag.Nivel is "A" or "S";
        }

        var checagens = new (string Id, bool Ok)[]
        {
            ("dividaZero", quitouDivida),
            ("reserva6m", reserva6m),
            ("nivelA", nivelA),
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

    // ---------- Estado do módulo Bolso ----------

    public async Task<FinancasDto> MontarFinancas(Personagem p)
    {
        var hoje = relogio.Hoje;
        var perfil = p.PerfilFinanceiro;
        var aportesDoMes = await AportesDoMes(p, hoje);
        var diag = perfil is null ? null : FormulasFinancas.Diagnostico(perfil, p.Economias, aportesDoMes);

        var dividas = await db.Dividas
            .Where(d => d.PersonagemId == p.Id)
            .OrderBy(d => d.QuitadaEm != null)
            .ThenByDescending(d => d.JurosPctMes) // avalanche: juros altos primeiro (PRD 4.1)
            .ToListAsync();

        var aportes = await db.Aportes
            .Where(a => a.PersonagemId == p.Id)
            .OrderByDescending(a => a.Data).ThenByDescending(a => a.Id)
            .Take(12).ToListAsync();

        var convertidoMes = await ConvertidoNoMes(p, hoje);
        var liberadoTotal = (decimal)await db.TransacoesMoedas
            .Where(t => t.PersonagemId == p.Id && t.Tipo == "conversao")
            .SumAsync(t => t.Valor) / FormulasFinancas.MoedasPorLibra;

        var metaBatida = perfil is not null && perfil.RendaMensal > 0
            && aportesDoMes >= perfil.RendaMensal * (decimal)(FormulasFinancas.MetaPoupancaPct / 100);

        return new FinancasDto(
            perfil is null ? null : new PerfilFinanceiroDto(perfil.RendaMensal, perfil.DespesasFixas, perfil.DespesasVariaveis, perfil.OrcamentoRecompensa),
            diag is null ? null : new DiagnosticoDto(
                diag.MesesReserva, diag.ReservaMeta, diag.ReservaFaltante,
                diag.TaxaPoupancaPct, FormulasFinancas.MetaPoupancaPct, diag.PctNecessidades, diag.PctDesejos,
                diag.Score, diag.Nivel, metaBatida),
            dividas.Select(d => new DividaDto(d.Id, d.Nome, d.ValorAtual, d.JurosPctMes, d.QuitadaEm != null)).ToList(),
            aportes.Select(a => new AporteDto(a.Id, a.Valor, a.Data.ToString("yyyy-MM-dd"))).ToList(),
            aportesDoMes,
            new ConversaoDto(convertidoMes, perfil?.OrcamentoRecompensa ?? 0, liberadoTotal),
            MontarConselhosFinancas(p, diag, dividas, aportesDoMes),
            p.AvisoFinancasAceitoEm is not null);
    }

    /// <summary>Conselhos de Nível 1 — regras determinísticas da tabela 4.1, sem IA.</summary>
    private List<string> MontarConselhosFinancas(Personagem p, DiagnosticoFinanceiro? diag, List<Divida> dividas, decimal aportesDoMes)
    {
        var conselhos = new List<string>();
        if (diag is null)
        {
            conselhos.Add("Preencha seu perfil financeiro (renda e despesas) para gerar o diagnóstico.");
            return conselhos;
        }

        if (diag.MesesReserva < FormulasFinancas.MesesReservaMeta)
            conselhos.Add($"Sua reserva cobre {diag.MesesReserva.ToString("0.#", CulturaPtBr)} meses. " +
                $"Faltam £{diag.ReservaFaltante.ToString("#,0", CulturaPtBr)} para a meta de {FormulasFinancas.MesesReservaMeta} meses.");
        else
            conselhos.Add($"Reserva de emergência completa ({diag.MesesReserva.ToString("0.#", CulturaPtBr)} meses) — agora todo aporte é construção de patrimônio.");

        if (diag.TaxaPoupancaPct < FormulasFinancas.MetaPoupancaPct)
        {
            var faltamPct = FormulasFinancas.MetaPoupancaPct - diag.TaxaPoupancaPct;
            var faltamLibras = p.PerfilFinanceiro!.RendaMensal * (decimal)(faltamPct / 100);
            conselhos.Add($"Você está poupando {diag.TaxaPoupancaPct.ToString("0.#", CulturaPtBr)}% da renda este mês. " +
                $"Suba para {FormulasFinancas.MetaPoupancaPct:0}% aportando mais £{faltamLibras.ToString("#,0", CulturaPtBr)}.");
        }

        var abertas = dividas.Where(d => d.QuitadaEm == null).OrderByDescending(d => d.JurosPctMes).ToList();
        if (abertas.Count > 1)
            conselhos.Add($"Método avalanche: quite primeiro \"{abertas[0].Nome}\" ({abertas[0].JurosPctMes.ToString("0.#", CulturaPtBr)}%/mês, os juros mais altos), depois as demais.");
        else if (abertas.Count == 1)
            conselhos.Add($"Concentre fogo na dívida \"{abertas[0].Nome}\" — quitar vira um chefe derrotado (+{DividaQuitadaXp} XP).");

        if (p.PerfilFinanceiro!.OrcamentoRecompensa <= 0)
            conselhos.Add("Defina um orçamento de recompensa para converter moedas em dinheiro livre de culpa (10 🪙 = £1).");

        return conselhos;
    }
}
