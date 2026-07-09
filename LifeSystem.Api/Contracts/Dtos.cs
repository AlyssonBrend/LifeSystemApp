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
public record FocoIniciarReq(string Tipo); // "foco" | "descanso"
public record FocoEncerrarReq(bool Abandonar);

// ---------- Estado ----------
public record PersonagemDto(
    string Nome, int Level, int XpAtual, int XpProximoLevel, long XpTotal,
    int Moedas, decimal Economias, int StreakDias, double MultiplicadorStreak,
    string? Classe, string Titulo, string EmojiTitulo,
    int Hp, int Energia, int DiasPerfeitos, int ChefesDerrotados,
    bool PodeEscolherClasse, bool BonusClasseAtivo);

public record AtributoDto(string Id, string Nome, string Emoji, int Valor, string Faixa, bool TemDados);

public record MissaoDto(
    string Id, string Nome, string Emoji, string Requisito,
    int XpBase, int XpFinal, int MoedasFinal, int DanoChefe,
    bool Concluida, bool BonusClasse,
    string[]? Checklist, bool[] Checks,
    int? MinutosNecessarios, int ProgressoMinutos);

public record ChefeDto(
    string Nome, string Emoji, string[] Ataques,
    int HpAtual, int HpMax, bool Enfurecido, string Status,
    string RecompensaCaixa, string ProximaSegunda);

public record ConquistaDto(string Id, string Nome, string Emoji, string Descricao, bool Desbloqueada);

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

// Eventos disparados por uma ação (o frontend usa para animações/toasts)
public record EventoDto(string Tipo, string? Titulo = null, int? Level = null, string? Nome = null, string? Emoji = null, string? Recompensa = null);

public record AcaoResp(EstadoDto Estado, List<EventoDto> Eventos);
