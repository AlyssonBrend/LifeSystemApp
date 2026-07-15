using System.Globalization;
using LifeSystem.Api.Contracts;
using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Services;

/// <summary>
/// Fase 2 — Corpo (PRD 4.2 e 4.3): dieta com metas calculadas, registro de cargas (PRs com 1RM),
/// cardio com pace por faixa, amigos por código e rankings. Mesma regra: tudo no backend.
/// </summary>
public partial class JogoService
{
    private const int PremioPrXp = 50;
    private const int PremioPrMoedas = 5;
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private const string LetrasCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sem 0/O/1/I

    private static MetasNutricionais? MetasDoPerfil(Personagem p) =>
        p.PerfilCorporal is { } perfil
            ? FormulasCorpo.Metas(perfil.PesoKg, perfil.AlturaCm, perfil.Idade, perfil.Sexo, perfil.Atividade, perfil.Objetivo)
            : null;

    // ---------- Perfil corporal e dieta (PRD 4.2) ----------

    public Task<List<EventoDto>> DefinirPerfilCorporal(Personagem p, PerfilReq req)
    {
        if (req.PesoKg is < 30 or > 300) throw new ArgumentException("Peso fora da faixa (30–300 kg)");
        if (req.AlturaCm is < 120 or > 230) throw new ArgumentException("Altura fora da faixa (120–230 cm)");
        if (req.Idade is < 10 or > 100) throw new ArgumentException("Idade fora da faixa (10–100)");
        if (req.Sexo is not ("m" or "f")) throw new ArgumentException("Sexo inválido");
        if (!FormulasCorpo.NiveisAtividade.Contains(req.Atividade)) throw new ArgumentException("Nível de atividade inválido");
        if (!FormulasCorpo.Objetivos.Contains(req.Objetivo)) throw new ArgumentException("Objetivo inválido");

        var perfil = p.PerfilCorporal;
        if (perfil is null)
        {
            perfil = new PerfilCorporal { PersonagemId = p.Id };
            p.PerfilCorporal = perfil;
            db.PerfisCorporais.Add(perfil);
        }
        perfil.PesoKg = req.PesoKg;
        perfil.AlturaCm = req.AlturaCm;
        perfil.Idade = req.Idade;
        perfil.Sexo = req.Sexo;
        perfil.Atividade = req.Atividade;
        perfil.Objetivo = req.Objetivo;
        perfil.AtualizadoEm = relogio.AgoraUtc;
        return Task.FromResult(new List<EventoDto>());
    }

    /// <summary>Disclaimer obrigatório (PRD 4.5): conselhos são educacionais, exigir aceite no primeiro uso.</summary>
    public Task<List<EventoDto>> AceitarAvisoSaude(Personagem p)
    {
        p.AvisoSaudeAceitoEm ??= relogio.AgoraUtc;
        return Task.FromResult(new List<EventoDto>());
    }

    // ---------- Registro de cargas — PRs (PRD 4.3) ----------

    public async Task<List<EventoDto>> RegistrarCarga(Personagem p, string exercicioId, double cargaKg, int reps)
    {
        var ex = CatalogoCorpo.Exercicios.FirstOrDefault(e => e.Id == exercicioId)
            ?? throw new ArgumentException("Exercício desconhecido");
        if (cargaKg is <= 0 or > 600) throw new ArgumentException("Carga fora da faixa (até 600 kg)");
        if (reps is < 1 or > 50) throw new ArgumentException("Repetições fora da faixa (1–50)");

        var hoje = relogio.Hoje;
        var rm1 = FormulasCorpo.Rm1Epley(cargaKg, reps);
        var melhorAnterior = await db.RegistrosCarga
            .Where(r => r.PersonagemId == p.Id && r.ExercicioId == exercicioId)
            .MaxAsync(r => (double?)r.Rm1);
        var ehPr = melhorAnterior is null || rm1 > melhorAnterior.Value;

        var registro = new RegistroCarga
        {
            PersonagemId = p.Id, ExercicioId = exercicioId,
            CargaKg = cargaKg, Reps = reps, Rm1 = rm1, Pr = ehPr, Data = hoje,
            CriadoEm = relogio.AgoraUtc,
        };
        db.RegistrosCarga.Add(registro);

        var eventos = new List<EventoDto>();
        if (ehPr)
        {
            // Máx. 1 PR premiado por exercício por semana, para não virar farm (PRD 4.3)
            var segunda = Formulas.SegundaFeiraDe(hoje);
            var jaPremiado = await db.RegistrosCarga.AnyAsync(r =>
                r.PersonagemId == p.Id && r.ExercicioId == exercicioId && r.PrPremiado && r.Data >= segunda);
            if (!jaPremiado)
            {
                registro.PrPremiado = true;
                GanharMoedas(p, PremioPrMoedas, $"pr:{exercicioId}");
                AplicarXp(p, PremioPrXp, eventos);
            }
            eventos.Add(new("novoRecorde", Nome: ex.Nome,
                Titulo: $"1RM estimado: {rm1.ToString("0.#", CulturaPtBr)} kg", Emoji: "💥"));
        }

        // "1 treino registrado" completa a missão do dia (PRD 3.3)
        eventos.AddRange(await ConcluirMissao(p, "treinar"));
        eventos.AddRange(await ChecarConquistasCorpo(p));
        return eventos;
    }

    // ---------- Cardio — pace e PRs por faixa (PRD 4.3) ----------

    public async Task<List<EventoDto>> RegistrarCardio(Personagem p, double distanciaKm, double duracaoMin)
    {
        if (distanciaKm is < 0.5 or > 300) throw new ArgumentException("Distância fora da faixa (0,5–300 km)");
        if (duracaoMin is < 3 or > 1440) throw new ArgumentException("Duração fora da faixa (3 min – 24 h)");
        var pace = FormulasCorpo.PaceSegPorKm(distanciaKm, duracaoMin);
        if (pace < 120) throw new ArgumentException("Pace abaixo de 2 min/km — confira distância e tempo");

        var hoje = relogio.Hoje;
        var faixa = FormulasCorpo.FaixaDe(distanciaKm);
        var ehPr = false;
        if (faixa is not null)
        {
            var melhorAnterior = await db.RegistrosCardio
                .Where(r => r.PersonagemId == p.Id && r.FaixaKm == faixa)
                .MinAsync(r => (int?)r.PaceSegKm);
            ehPr = melhorAnterior is null || pace < melhorAnterior.Value;
        }

        var registro = new RegistroCardio
        {
            PersonagemId = p.Id, DistanciaKm = distanciaKm, DuracaoMin = duracaoMin,
            PaceSegKm = pace, FaixaKm = faixa, Pr = ehPr, Data = hoje,
            CriadoEm = relogio.AgoraUtc,
        };
        db.RegistrosCardio.Add(registro);

        var eventos = new List<EventoDto>();
        if (ehPr)
        {
            var segunda = Formulas.SegundaFeiraDe(hoje);
            var jaPremiado = await db.RegistrosCardio.AnyAsync(r =>
                r.PersonagemId == p.Id && r.FaixaKm == faixa && r.PrPremiado && r.Data >= segunda);
            if (!jaPremiado)
            {
                registro.PrPremiado = true;
                GanharMoedas(p, PremioPrMoedas, $"pr:cardio{faixa}k");
                AplicarXp(p, PremioPrXp, eventos);
            }
            eventos.Add(new("novoRecorde", Nome: $"Corrida — faixa de {faixa} km",
                Titulo: $"Pace: {FormatarPace(pace)}/km", Emoji: "💥"));
        }

        // Corridas alimentam a missão de treino do dia (PRD 4.3)
        eventos.AddRange(await ConcluirMissao(p, "treinar"));
        eventos.AddRange(await ChecarConquistasCorpo(p));
        return eventos;
    }

    private static string FormatarPace(int segKm) => $"{segKm / 60}:{segKm % 60:00}";

    /// <summary>Conquistas de força e cardio (PRD 4.3) — checadas só nas ações do módulo Corpo.</summary>
    private async Task<List<EventoDto>> ChecarConquistasCorpo(Personagem p)
    {
        var eventos = new List<EventoDto>();
        var desbloqueadas = p.Conquistas.Select(c => c.ConquistaId).ToHashSet();
        var hoje = relogio.Hoje;

        // Registros desta requisição ainda não foram salvos — une o change tracker ao banco
        var cargasLocal = db.RegistrosCarga.Local.Where(r => r.PersonagemId == p.Id).ToList();
        var cardiosLocal = db.RegistrosCardio.Local.Where(r => r.PersonagemId == p.Id).ToList();

        async Task<double> MelhorRm1(string exercicioId)
        {
            var noBanco = await db.RegistrosCarga
                .Where(r => r.PersonagemId == p.Id && r.ExercicioId == exercicioId)
                .MaxAsync(r => (double?)r.Rm1) ?? 0;
            var local = cargasLocal.Where(r => r.ExercicioId == exercicioId)
                .Select(r => r.Rm1).DefaultIfEmpty(0).Max();
            return Math.Max(noBanco, local);
        }

        var temPr = cargasLocal.Any(r => r.Pr) || cardiosLocal.Any(r => r.Pr)
            || await db.RegistrosCarga.AnyAsync(r => r.PersonagemId == p.Id && r.Pr)
            || await db.RegistrosCardio.AnyAsync(r => r.PersonagemId == p.Id && r.Pr);
        var peso = p.PerfilCorporal?.PesoKg ?? 0;
        var melhorSupino = desbloqueadas.Contains("supinoCorpo") ? 0 : await MelhorRm1("supino");
        var melhorAgacho = desbloqueadas.Contains("agacha100") ? 0 : await MelhorRm1("agachamento");

        bool TemCardio(Func<RegistroCardio, bool> criterio) => cardiosLocal.Any(criterio);
        var tem5k = TemCardio(r => r.DistanciaKm >= 5)
            || await db.RegistrosCardio.AnyAsync(r => r.PersonagemId == p.Id && r.DistanciaKm >= 5);
        var tem10kEm60 = TemCardio(r => r.DistanciaKm >= 10 && r.DuracaoMin <= 60)
            || await db.RegistrosCardio.AnyAsync(r => r.PersonagemId == p.Id && r.DistanciaKm >= 10 && r.DuracaoMin <= 60);
        var temMeia = TemCardio(r => r.DistanciaKm >= 21)
            || await db.RegistrosCardio.AnyAsync(r => r.PersonagemId == p.Id && r.DistanciaKm >= 21);
        var inicio30 = hoje.AddDays(-29);
        var kmMes = await db.RegistrosCardio
                        .Where(r => r.PersonagemId == p.Id && r.Data >= inicio30)
                        .SumAsync(r => r.DistanciaKm)
                    + cardiosLocal.Where(r => r.Data >= inicio30 && r.Id == 0).Sum(r => r.DistanciaKm);

        var checagens = new (string Id, bool Ok)[]
        {
            ("primeiroPr", temPr),
            ("supinoCorpo", peso > 0 && melhorSupino >= peso),
            ("agacha100", melhorAgacho >= 100),
            ("corrida5k", tem5k),
            ("dez60", tem10kEm60),
            ("km100mes", kmMes >= 100),
            ("meia21k", temMeia),
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

    // ---------- Amigos e rankings (PRD 4.3) ----------

    public async Task<List<EventoDto>> AdicionarAmigo(Personagem p, string codigo)
    {
        codigo = codigo.Trim().ToUpperInvariant();
        var alvo = await db.Personagens.FirstOrDefaultAsync(x => x.CodigoAmigo == codigo)
            ?? throw new ArgumentException("Código não encontrado — confira com seu amigo");
        if (alvo.Id == p.Id) throw new ArgumentException("Esse é o seu próprio código");

        var jaExiste = await db.Amizades.AnyAsync(a =>
            (a.SolicitanteId == p.Id && a.ConvidadoId == alvo.Id) ||
            (a.SolicitanteId == alvo.Id && a.ConvidadoId == p.Id));
        if (jaExiste) throw new InvalidOperationException("Vocês já têm um convite ou amizade");

        db.Amizades.Add(new Amizade { SolicitanteId = p.Id, ConvidadoId = alvo.Id, CriadaEm = relogio.AgoraUtc });
        return [new("convite", Nome: alvo.Nome)];
    }

    public async Task<List<EventoDto>> ResponderAmizade(Personagem p, int amizadeId, bool aceitar)
    {
        var amizade = await db.Amizades.FirstOrDefaultAsync(a =>
                a.Id == amizadeId && a.ConvidadoId == p.Id && a.Status == "pendente")
            ?? throw new ArgumentException("Convite não encontrado");
        if (aceitar) amizade.Status = "aceita"; // aceitar cria a amizade (PRD 4.3)
        else db.Amizades.Remove(amizade);
        return [];
    }

    public Task<List<EventoDto>> DefinirRankingOptIn(Personagem p, bool optIn)
    {
        p.RankingOptIn = optIn; // participar dos rankings é opt-in (PRD 4.3)
        return Task.FromResult(new List<EventoDto>());
    }

    private async Task GarantirCodigoAmigo(Personagem p)
    {
        while (p.CodigoAmigo is null)
        {
            var codigo = new string(Enumerable.Range(0, 6)
                .Select(_ => LetrasCodigo[Random.Shared.Next(LetrasCodigo.Length)]).ToArray());
            if (!await db.Personagens.AnyAsync(x => x.CodigoAmigo == codigo))
                p.CodigoAmigo = codigo;
        }
    }

    // ---------- Estado do módulo Corpo ----------

    public async Task<CorpoDto> MontarCorpo(Personagem p)
    {
        await GarantirCodigoAmigo(p);
        var hoje = relogio.Hoje;
        var inicio30 = hoje.AddDays(-29);

        var perfil = p.PerfilCorporal;
        var metas = MetasDoPerfil(p);
        var plano = metas is null ? [] : FormulasCorpo.PlanoRefeicoes(metas);

        var cargas = await db.RegistrosCarga
            .Where(r => r.PersonagemId == p.Id)
            .OrderByDescending(r => r.Data).ThenByDescending(r => r.Id)
            .Take(500).ToListAsync();
        var cardios = await db.RegistrosCardio
            .Where(r => r.PersonagemId == p.Id)
            .OrderByDescending(r => r.Data).ThenByDescending(r => r.Id)
            .Take(500).ToListAsync();
        var kmMes = cardios.Where(r => r.Data >= inicio30).Sum(r => r.DistanciaKm);

        var exercicios = CatalogoCorpo.Exercicios.Select(e =>
        {
            var melhor = cargas.Where(r => r.ExercicioId == e.Id).MaxBy(r => r.Rm1);
            return new ExercicioDto(e.Id, e.Nome, e.Emoji, e.Grupo, e.Basico,
                melhor is null ? null : Math.Round(melhor.Rm1, 1),
                melhor is null ? null : $"{melhor.CargaKg.ToString("0.#", CulturaPtBr)} kg × {melhor.Reps}",
                melhor?.Data.ToString("yyyy-MM-dd"));
        }).ToList();

        var faixasCardio = CatalogoCorpo.FaixasCardioKm.Select(f =>
        {
            var melhor = cardios.Where(r => r.FaixaKm == f).MinBy(r => r.PaceSegKm);
            return new FaixaCardioDto(f, melhor?.PaceSegKm, melhor?.Data.ToString("yyyy-MM-dd"));
        }).ToList();

        var fichas = CatalogoCorpo.Fichas
            .Select(f => new FichaDto(f.Id, f.Nome, f.Objetivo, f.Frequencia,
                f.Dias.Select(d => new FichaDiaDto(d.Nome, d.Exercicios)).ToList()))
            .ToList();

        var amizades = await db.Amizades
            .Where(a => a.SolicitanteId == p.Id || a.ConvidadoId == p.Id)
            .ToListAsync();
        var idsOutros = amizades.Select(a => a.SolicitanteId == p.Id ? a.ConvidadoId : a.SolicitanteId).ToList();
        var nomes = await db.Personagens
            .Where(x => idsOutros.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Nome);
        var amigos = amizades.Select(a =>
        {
            var outroId = a.SolicitanteId == p.Id ? a.ConvidadoId : a.SolicitanteId;
            var situacao = a.Status == "aceita" ? "aceita"
                : a.SolicitanteId == p.Id ? "pendenteEnviado" : "pendenteRecebido";
            return new AmigoDto(a.Id, nomes.GetValueOrDefault(outroId, "?"), situacao);
        }).ToList();

        var idsAmigos = amizades.Where(a => a.Status == "aceita")
            .Select(a => a.SolicitanteId == p.Id ? a.ConvidadoId : a.SolicitanteId)
            .Append(p.Id).ToList();
        var (rankForca, rankCardio) = await MontarRankings(p, idsAmigos);

        return new CorpoDto(
            perfil is null ? null : new PerfilCorporalDto(perfil.PesoKg, perfil.AlturaCm, perfil.Idade, perfil.Sexo, perfil.Atividade, perfil.Objetivo),
            metas is null ? null : new MetasDto(metas.Calorias, metas.ProteinaG, metas.GorduraG, metas.CarboG, metas.FibrasG, metas.AguaMl),
            plano.Select(r => new RefeicaoDto(r.Nome, r.Kcal, r.Sugestao)).ToList(),
            exercicios,
            cargas.Take(60).Select(r => new RegistroCargaDto(r.Id, r.ExercicioId, r.CargaKg, r.Reps, Math.Round(r.Rm1, 1), r.Pr, r.Data.ToString("yyyy-MM-dd"))).ToList(),
            fichas,
            faixasCardio,
            cardios.Take(60).Select(r => new RegistroCardioDto(r.Id, r.DistanciaKm, r.DuracaoMin, r.PaceSegKm, r.FaixaKm, r.Pr, r.Data.ToString("yyyy-MM-dd"))).ToList(),
            Math.Round(kmMes, 1),
            MontarConselhos(p, metas, cargas, kmMes),
            p.CodigoAmigo!,
            p.RankingOptIn,
            p.AvisoSaudeAceitoEm is not null,
            amigos,
            rankForca,
            rankCardio);
    }

    /// <summary>Conselhos de Nível 1 — regras determinísticas, sem IA (PRD seção 4).</summary>
    private List<string> MontarConselhos(Personagem p, MetasNutricionais? metas, List<RegistroCarga> cargas, double kmMes)
    {
        var conselhos = new List<string>();
        var hoje = relogio.Hoje;

        if (metas is null)
            conselhos.Add("Preencha seu perfil corporal para calcular calorias, macros e o plano de refeições.");
        else
            conselhos.Add(p.PerfilCorporal!.Objetivo switch
            {
                "emagrecer" => $"Déficit de 20%: mire {metas.Calorias} kcal/dia. Proteína alta ({metas.ProteinaG}g) protege a massa magra.",
                "ganhar" => $"Superávit de 10%: mire {metas.Calorias} kcal/dia e bata os {metas.ProteinaG}g de proteína todos os dias.",
                _ => $"Manutenção: {metas.Calorias} kcal/dia com {metas.ProteinaG}g de proteína mantém corpo e treino em equilíbrio.",
            });

        // Progressão (PRD 4.3): mesma carga máxima em 3+ sessões nas últimas 3 semanas → hora de subir
        var inicio21 = hoje.AddDays(-20);
        foreach (var grupo in cargas.Where(r => r.Data >= inicio21).GroupBy(r => r.ExercicioId))
        {
            var cargaMax = grupo.Max(r => r.CargaKg);
            var sessoesNaMax = grupo.Where(r => r.CargaKg == cargaMax).Select(r => r.Data).Distinct().Count();
            if (sessoesNaMax >= 3)
            {
                var nome = CatalogoCorpo.Exercicios.First(e => e.Id == grupo.Key).Nome;
                conselhos.Add($"Você repetiu {cargaMax.ToString("0.#", CulturaPtBr)} kg no {nome} por {sessoesNaMax} sessões — tente +2,5 kg ou +1 repetição.");
            }
        }

        if (cargas.Count == 0)
            conselhos.Add("Registre suas cargas nos básicos (supino, agachamento, terra) para a Força virar medição real.");
        if (kmMes == 0)
            conselhos.Add("Nenhuma corrida em 30 dias — registre uma corrida para a Resistência virar medição real.");

        return conselhos;
    }

    private async Task<(List<RankingDto> Forca, List<RankingDto> Cardio)> MontarRankings(Personagem p, List<int> idsAmigos)
    {
        // Melhor 1RM por (personagem, exercício) — amigos sempre; geral só com opt-in (PRD 4.3)
        var brutoForca = await db.RegistrosCarga
            .GroupBy(r => new { r.PersonagemId, r.ExercicioId })
            .Select(g => new { g.Key.PersonagemId, g.Key.ExercicioId, Rm1 = g.Max(r => r.Rm1) })
            .ToListAsync();
        var brutoCardio = await db.RegistrosCardio
            .Where(r => r.FaixaKm != null)
            .GroupBy(r => new { r.PersonagemId, r.FaixaKm })
            .Select(g => new { g.Key.PersonagemId, Faixa = g.Key.FaixaKm!.Value, Pace = g.Min(r => r.PaceSegKm) })
            .ToListAsync();

        var idsEnvolvidos = brutoForca.Select(b => b.PersonagemId)
            .Concat(brutoCardio.Select(b => b.PersonagemId)).Distinct().ToList();
        var jogadores = await db.Personagens
            .Where(x => idsEnvolvidos.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome, x.RankingOptIn, Peso = x.PerfilCorporal == null ? 0 : x.PerfilCorporal.PesoKg })
            .ToDictionaryAsync(x => x.Id);

        List<RankingEntradaDto> Entradas<T>(IEnumerable<T> fonte, Func<T, int> idDe, Func<T, double> valorDe,
            bool relativoAoPeso, bool menorMelhor, Func<int, bool> escopo) =>
            fonte.Where(b => jogadores.ContainsKey(idDe(b)) && escopo(idDe(b)))
                .Select(b =>
                {
                    var j = jogadores[idDe(b)];
                    double? rel = relativoAoPeso && j.Peso > 0 ? Math.Round(valorDe(b) / j.Peso, 2) : null;
                    return new RankingEntradaDto(j.Nome, Math.Round(valorDe(b), 1), rel, idDe(b) == p.Id);
                })
                .OrderByDescending(e => menorMelhor ? -e.Valor : e.Relativo ?? e.Valor * 0.001)
                .Take(10).ToList();

        bool EhAmigo(int id) => idsAmigos.Contains(id);
        // Ranking geral com selo "auto-relatado" no frontend; só quem deu opt-in aparece
        bool NoGeral(int id) => jogadores[id].RankingOptIn;

        var rankingsForca = CatalogoCorpo.Exercicios
            .Where(e => brutoForca.Any(b => b.ExercicioId == e.Id))
            .Select(e => new RankingDto(e.Id, e.Nome,
                Entradas(brutoForca.Where(b => b.ExercicioId == e.Id), b => b.PersonagemId, b => b.Rm1, true, false, EhAmigo),
                Entradas(brutoForca.Where(b => b.ExercicioId == e.Id), b => b.PersonagemId, b => b.Rm1, true, false, NoGeral)))
            .ToList();

        var rankingsCardio = CatalogoCorpo.FaixasCardioKm
            .Where(f => brutoCardio.Any(b => b.Faixa == f))
            .Select(f => new RankingDto($"cardio{f}", $"Corrida — {f} km",
                Entradas(brutoCardio.Where(b => b.Faixa == f), b => b.PersonagemId, b => (double)b.Pace, false, true, EhAmigo),
                Entradas(brutoCardio.Where(b => b.Faixa == f), b => b.PersonagemId, b => (double)b.Pace, false, true, NoGeral)))
            .ToList();

        return (rankingsForca, rankingsCardio);
    }
}
