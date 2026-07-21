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

    // Fase 2 — Corpo
    public string? CodigoAmigo { get; set; }        // código de convite (PRD 4.3); gerado no primeiro acesso ao módulo
    public bool RankingOptIn { get; set; }          // participar dos rankings é opt-in (PRD 4.3)
    public DateTime? AvisoSaudeAceitoEm { get; set; } // aceite do disclaimer de dieta/treino (PRD 4.5 ⚠️)

    // Fase 3 — Mente e Bolso
    public DateTime? AvisoFinancasAceitoEm { get; set; } // aceite do disclaimer financeiro (PRD 4.5 ⚠️ — por módulo)

    public List<MissaoLog> Missoes { get; set; } = [];
    public List<ChefeInstancia> Chefes { get; set; } = [];
    public List<ConquistaDesbloqueada> Conquistas { get; set; } = [];
    public List<ItemLoja> Loja { get; set; } = [];
    public List<TransacaoMoedas> Transacoes { get; set; } = [];
    public List<SessaoFoco> SessoesFoco { get; set; } = [];
    public PerfilCorporal? PerfilCorporal { get; set; }
    public PerfilFinanceiro? PerfilFinanceiro { get; set; }
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

// ---------- Fase 2 — Corpo (PRD 4.2 e 4.3) ----------

/// <summary>Dados corporais do jogador — base do cálculo de metas nutricionais e da força relativa.</summary>
public class PerfilCorporal
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public double PesoKg { get; set; }
    public int AlturaCm { get; set; }
    public int Idade { get; set; }
    public string Sexo { get; set; } = "m";          // m | f
    public string Atividade { get; set; } = "leve";  // sedentario | leve | moderado | intenso | atleta
    public string Objetivo { get; set; } = "manter"; // emagrecer | manter | ganhar
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Registro de carga (LiftRecord): carga × reps → 1RM estimado (Epley). Histórico de PRs.</summary>
public class RegistroCarga
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string ExercicioId { get; set; } = "";
    public double CargaKg { get; set; }
    public int Reps { get; set; }
    public double Rm1 { get; set; }        // calculado no backend, nunca no frontend
    public bool Pr { get; set; }           // era recorde no momento do registro
    public bool PrPremiado { get; set; }   // máx. 1 PR premiado por exercício por semana (PRD 4.3)
    public DateOnly Data { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Registro de corrida (CardioLog): distância + tempo → pace; PR por faixa de distância.</summary>
public class RegistroCardio
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public double DistanciaKm { get; set; }
    public double DuracaoMin { get; set; }
    public int PaceSegKm { get; set; }     // calculado no backend
    public int? FaixaKm { get; set; }      // 1/5/10/15/21/42 — maior faixa coberta; null se < 1 km
    public bool Pr { get; set; }
    public bool PrPremiado { get; set; }   // máx. 1 PR premiado por faixa por semana (PRD 4.3)
    public DateOnly Data { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Amizade por código de convite (PRD 4.3): aceitar cria o vínculo — sem feed social.</summary>
public class Amizade
{
    public int Id { get; set; }
    public int SolicitanteId { get; set; }
    public int ConvidadoId { get; set; }
    public string Status { get; set; } = "pendente"; // pendente | aceita
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
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
    public int? HabilidadeId { get; set; }       // Fase 3: tempo creditado à habilidade estudada (PRD 4.4)
}

// ---------- Fase 3 — Mente e Bolso (PRD 4.1 e 4.4) ----------

/// <summary>Dados financeiros do jogador (PRD 4.1) — base do diagnóstico e do orçamento de recompensa (PRD 3.8).</summary>
public class PerfilFinanceiro
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public decimal RendaMensal { get; set; }
    public decimal DespesasFixas { get; set; }
    public decimal DespesasVariaveis { get; set; }
    public decimal OrcamentoRecompensa { get; set; } // teto mensal da conversão de moedas (PRD 3.8)
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Dívida para o método avalanche (PRD 4.1) — quitada vira um "chefe" derrotado.</summary>
public class Divida
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string Nome { get; set; } = "";
    public decimal ValorAtual { get; set; }
    public double JurosPctMes { get; set; }
    public DateTime? QuitadaEm { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Movimento das economias (positivo = aporte, negativo = retirada). Alimenta a taxa de poupança do mês.</summary>
public class AporteEconomia
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public decimal Valor { get; set; }
    public DateOnly Data { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Habilidade da árvore de conhecimento (PRD 4.4). TrilhaId aponta para o catálogo; null = personalizada.</summary>
public class Habilidade
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string? TrilhaId { get; set; }
    public string Nome { get; set; } = "";
    public string Emoji { get; set; } = "📖";
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public List<MarcoConcluido> Marcos { get; set; } = [];
}

/// <summary>Marco de trilha do catálogo concluído manualmente (personalizadas destravam por horas de foco).</summary>
public class MarcoConcluido
{
    public int Id { get; set; }
    public int HabilidadeId { get; set; }
    public int MarcoIndice { get; set; }
    public DateTime ConcluidoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Livro da lista de leitura — concluído alimenta o atributo 📚 Conhecimento (PRD 3.1).</summary>
public class Livro
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string Titulo { get; set; } = "";
    public int? HabilidadeId { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Interação social do dia (1 toque) — consistência em 30 dias alimenta o 🤝 Carisma (PRD 3.1).</summary>
public class InteracaoSocial
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public DateOnly Data { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ---------- Fase 4 — IA Mentora (PRD 4.5) ----------

/// <summary>Conselho gerado pela IA Mentora (AdviceLog do PRD) — histórico auditável e limite diário.</summary>
public class ConselhoMentor
{
    public int Id { get; set; }
    public int PersonagemId { get; set; }
    public string Conteudo { get; set; } = "";
    public DateOnly Data { get; set; }           // dia do jogador — base do limite diário
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
