namespace LifeSystem.Api.Contracts;

// ---------- Auth ----------
public record RegistrarReq(string Email, string Senha, string NomePersonagem);
public record LoginReq(string Email, string Senha);
public record AuthResp(string Token, string Email, string NomePersonagem);

// ---------- Ações ----------
public record CheckReq(int Indice, bool Marcado);
public record ClasseReq(string Classe);
public record RecompensaReq(string Texto);
public record EconomiasReq(decimal Valor);
public record ItemLojaReq(string Nome, int Preco);
public record FocoIniciarReq(string Tipo, int? HabilidadeId = null); // "foco" | "descanso" · Fase 3: crédito por habilidade
public record FocoEncerrarReq(bool Abandonar);

// ---------- Ações da Fase 2 — Corpo ----------
public record PerfilReq(double PesoKg, int AlturaCm, int Idade, string Sexo, string Atividade, string Objetivo);
public record CargaReq(string ExercicioId, double CargaKg, int Reps);
public record CardioReq(double DistanciaKm, double DuracaoMin);
public record AmigoReq(string Codigo);
public record ResponderAmizadeReq(bool Aceitar);
public record RankingOptInReq(bool OptIn);

// ---------- Ações da Fase 3 — Mente e Bolso ----------
public record PerfilFinanceiroReq(decimal RendaMensal, decimal DespesasFixas, decimal DespesasVariaveis, decimal OrcamentoRecompensa);
public record AporteReq(decimal Valor);
public record DividaReq(string Nome, decimal Valor, double JurosPctMes);
public record PagarDividaReq(decimal Valor);
public record ConverterReq(int Moedas);
public record HabilidadeReq(string? TrilhaId, string? Nome); // trilha do catálogo OU nome personalizado
public record LivroReq(string Titulo, int? HabilidadeId);

// ---------- Estado ----------
public record PersonagemDto(
    string Nome, int Level, int XpAtual, int XpProximoLevel, long XpTotal,
    int Moedas, decimal Economias, int StreakDias, int ProtecoesStreak, double MultiplicadorStreak,
    string? Classe, string Titulo, string EmojiTitulo,
    int Hp, int Energia, int DiasPerfeitos, int ChefesDerrotados,
    bool PodeEscolherClasse, bool BonusClasseAtivo, bool AvatarTranscendente);

public record AtributoDto(string Id, string Nome, string Emoji, int Valor, string Faixa, bool TemDados);

public record MissaoDto(
    string Id, string Nome, string Emoji, string Requisito,
    int XpBase, int XpFinal, int MoedasFinal, int DanoChefe,
    bool Concluida, bool BonusClasse,
    string[]? Checklist, bool[] Checks,
    int? MinutosNecessarios, int ProgressoMinutos, bool DeClasse);

public record ChefeDto(
    string Nome, string Emoji, string[] Ataques,
    int HpAtual, int HpMax, bool Enfurecido, string Status,
    string RecompensaCaixa, string ProximaSegunda);

public record ConquistaDto(string Id, string Nome, string Emoji, string Descricao, bool Desbloqueada, bool Oculta);

public record ItemLojaDto(int Id, string Nome, int Preco);

public record CompraDto(string Nome, int Valor, DateTime Em);

public record FocoDto(string Tipo, DateTime IniciadaEm, int DuracaoSegundos, int DecorridoSegundos);

public record EstadoDto(
    PersonagemDto Personagem,
    List<AtributoDto> Atributos,
    List<MissaoDto> MissoesHoje,
    ChefeDto Chefe,
    List<ConquistaDto> Conquistas,
    List<ItemLojaDto> Loja,
    List<CompraDto> Compras,
    FocoDto? FocoAtivo,
    DateTime AgoraServidor);

// ---------- Estado da Fase 2 — Corpo ----------

public record PerfilCorporalDto(double PesoKg, int AlturaCm, int Idade, string Sexo, string Atividade, string Objetivo);

public record MetasDto(int Calorias, int ProteinaG, int GorduraG, int CarboG, int FibrasG, int AguaMl);

public record RefeicaoDto(string Nome, int Kcal, string Sugestao);

public record ExercicioDto(
    string Id, string Nome, string Emoji, string Grupo, bool Basico,
    double? MelhorRm1, string? MelhorMarca, string? MelhorEm);

public record RegistroCargaDto(int Id, string ExercicioId, double CargaKg, int Reps, double Rm1, bool Pr, string Data);

public record FichaDiaDto(string Nome, string[] Exercicios);

public record FichaDto(string Id, string Nome, string Objetivo, string Frequencia, List<FichaDiaDto> Dias);

public record FaixaCardioDto(int FaixaKm, int? MelhorPaceSegKm, string? MelhorEm);

public record RegistroCardioDto(int Id, double DistanciaKm, double DuracaoMin, int PaceSegKm, int? FaixaKm, bool Pr, string Data);

public record AmigoDto(int AmizadeId, string Nome, string Situacao); // pendenteEnviado | pendenteRecebido | aceita

public record RankingEntradaDto(string Nome, double Valor, double? Relativo, bool EhVoce);

public record RankingDto(string Chave, string Titulo, List<RankingEntradaDto> Amigos, List<RankingEntradaDto> Geral);

public record CorpoDto(
    PerfilCorporalDto? Perfil, MetasDto? Metas, List<RefeicaoDto> Plano,
    List<ExercicioDto> Exercicios, List<RegistroCargaDto> Cargas, List<FichaDto> Fichas,
    List<FaixaCardioDto> FaixasCardio, List<RegistroCardioDto> Cardios, double KmMes,
    List<string> Conselhos, string CodigoAmigo, bool RankingOptIn, bool AvisoSaudeAceito,
    List<AmigoDto> Amigos, List<RankingDto> RankingsForca, List<RankingDto> RankingsCardio);

// ---------- Estado da Fase 3 — Bolso ----------

public record PerfilFinanceiroDto(decimal RendaMensal, decimal DespesasFixas, decimal DespesasVariaveis, decimal OrcamentoRecompensa);

public record DiagnosticoDto(
    double MesesReserva, decimal ReservaMeta, decimal ReservaFaltante,
    double TaxaPoupancaPct, double MetaPoupancaPct, double PctNecessidades, double PctDesejos,
    int Score, string Nivel, bool MetaDoMesBatida);

public record DividaDto(int Id, string Nome, decimal ValorAtual, double JurosPctMes, bool Quitada);

public record AporteDto(int Id, decimal Valor, string Data);

public record ConversaoDto(decimal ConvertidoMesLibras, decimal TetoMesLibras, decimal LiberadoTotalLibras);

public record FinancasDto(
    PerfilFinanceiroDto? Perfil, DiagnosticoDto? Diagnostico,
    List<DividaDto> Dividas, List<AporteDto> Aportes, decimal AportesDoMes,
    ConversaoDto Conversao, List<string> Conselhos, bool AvisoAceito);

// ---------- Estado da Fase 3 — Mente ----------

public record TrilhaDto(string Id, string Nome, string Emoji, string[] Marcos, bool JaAdicionada);

public record MarcoDto(int Indice, string Nome, bool Concluido);

public record HabilidadeDto(
    int Id, string? TrilhaId, string Nome, string Emoji,
    double HorasFoco, List<MarcoDto> Marcos);

public record LivroDto(int Id, string Titulo, int? HabilidadeId, bool Concluido);

public record MenteDto(
    List<HabilidadeDto> Habilidades, List<TrilhaDto> Trilhas,
    List<LivroDto> Livros, int LivrosConcluidos, int MarcosConcluidos,
    bool InteracaoHoje, int DiasSociais30, List<string> Conselhos);

// ---------- Estado da Fase 4 — IA Mentora ----------

public record ConselhoMentorDto(int Id, string Conteudo, DateTime CriadoEm);

public record MentorDto(
    bool Configurado, int LimiteDiario, int RestantesHoje,
    List<ConselhoMentorDto> Historico);

// Eventos disparados por uma ação (o frontend usa para animações/toasts)
public record EventoDto(string Tipo, string? Titulo = null, int? Level = null, string? Nome = null, string? Emoji = null, string? Recompensa = null);

public record AcaoResp(EstadoDto Estado, List<EventoDto> Eventos, CorpoDto? Corpo = null, FinancasDto? Financas = null, MenteDto? Mente = null, MentorDto? Mentor = null);
