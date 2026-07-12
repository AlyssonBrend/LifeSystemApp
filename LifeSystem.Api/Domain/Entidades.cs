namespace LifeSystem.Api.Domain;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Personagem Personagem { get; set; } = null!;
}

public class Personagem
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nome { get; set; } = "";
    public int Level { get; set; }               // inicia em 0 (PRD 3.2)
    public int XpAtual { get; set; }             // XP dentro do level atual
    public long XpTotal { get; set; }
    public int Moedas { get; set; }
    public decimal Economias { get; set; }
    public int StreakDias { get; set; }
    public DateOnly? UltimoDiaComMissao { get; set; }
    public int ProtecoesStreak { get; set; }     // dia perfeito concede +1 (PRD 3.3); cobre dias perdidos
    public string? Classe { get; set; }          // escolhida no level 5 (PRD 3.5)
    public DateTime? ClasseEscolhidaEm { get; set; } // carência de 30 dias entre trocas (PRD 3.5)
    public DateTime? TranscendenciaDesde { get; set; } // desde quando os 10 atributos estão em 80+
    public bool AvatarTranscendente { get; set; }      // classe oculta (PRD 3.5): +10% XP permanente
    public string RecompensaCaixa { get; set; } = "";
    public int DiasPerfeitos { get; set; }
    public int ChefesDerrotados { get; set; }

    public List<MissaoLog> Missoes { get; set; } = [];
    public List<ChefeInstancia> Chefes { get; set; } = [];
    public List<ConquistaDesbloqueada> Conquistas { get; set; } = [];
    public List<ItemLoja> Loja { get; set; } = [];
    public List<TransacaoMoedas> Transacoes { get; set; } = [];
    public List<SessaoFoco> SessoesFoco { get; set; } = [];
}

public class MissaoLog
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string MissaoId { get; set; } = "";   // treinar, alimentacao, estudar, espiritualidade, trabalhar, dormir
    public DateOnly Data { get; set; }
    public bool Concluida { get; set; }
    public string ChecksJson { get; set; } = "[]";
    public int ProgressoMinutos { get; set; }    // usado pela missão estudar (Modo Foco)
    public DateTime? ConcluidaEm { get; set; }
}

public class ChefeInstancia
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public int ChefeIndice { get; set; }         // rotação PRD 3.4
    public DateOnly SemanaInicio { get; set; }   // segunda-feira
    public int HpAtual { get; set; }
    public int HpMax { get; set; }
    public bool Enfurecido { get; set; }         // derrota anterior: +10% HP e nome "Enfurecida"
    public string Status { get; set; } = "ativa"; // ativa | vencida | perdida
}

public class ConquistaDesbloqueada
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string ConquistaId { get; set; } = "";
    public DateTime DesbloqueadaEm { get; set; } = DateTime.UtcNow;
}

public class ItemLoja
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string Nome { get; set; } = "";
    public int Preco { get; set; }
    public bool Ativo { get; set; } = true;
}

// Extrato auditável (PRD 3.8 / entidade CoinTransaction)
public class TransacaoMoedas
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string Tipo { get; set; } = "";       // ganho | gasto
    public int Valor { get; set; }
    public string Origem { get; set; } = "";     // ex.: missao:treinar, levelup, chefe, conquista:streak7, loja:Jantar fora
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// Timestamps no servidor — o cronômetro do frontend é só exibição (PRD 3.9, anti-trapaça)
public class SessaoFoco
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string Tipo { get; set; } = "foco";   // foco (50min) | descanso (10min)
    public DateTime IniciadaEm { get; set; }
    public DateTime? EncerradaEm { get; set; }
    public string Status { get; set; } = "ativa"; // ativa | completa | abandonada
}
