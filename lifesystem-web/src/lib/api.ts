// Client HTTP da LifeSystem.Api. O frontend não calcula nenhuma regra de jogo —
// só exibe o estado que o servidor devolve (PRD seção 6).

const BASE = import.meta.env.VITE_API_URL ?? ''

// ---------- Tipos (espelho dos DTOs da API) ----------

export interface PersonagemDto {
  nome: string; level: number; xpAtual: number; xpProximoLevel: number; xpTotal: number
  moedas: number; economias: number; streakDias: number; protecoesStreak: number; multiplicadorStreak: number
  classe: string | null; titulo: string; emojiTitulo: string
  hp: number; energia: number; diasPerfeitos: number; chefesDerrotados: number
  podeEscolherClasse: boolean; bonusClasseAtivo: boolean; avatarTranscendente: boolean
}

export interface AtributoDto { id: string; nome: string; emoji: string; valor: number; faixa: string; temDados: boolean }

export interface MissaoDto {
  id: string; nome: string; emoji: string; requisito: string
  xpBase: number; xpFinal: number; moedasFinal: number; danoChefe: number
  concluida: boolean; bonusClasse: boolean
  checklist: string[] | null; checks: boolean[]
  minutosNecessarios: number | null; progressoMinutos: number; deClasse: boolean
}

export interface ChefeDto {
  nome: string; emoji: string; ataques: string[]
  hpAtual: number; hpMax: number; enfurecido: boolean; status: string
  recompensaCaixa: string; proximaSegunda: string
}

export interface ConquistaDto { id: string; nome: string; emoji: string; descricao: string; desbloqueada: boolean; oculta: boolean }
export interface ItemLojaDto { id: number; nome: string; preco: number }
export interface CompraDto { nome: string; valor: number; em: string }
export interface FocoDto { tipo: 'foco' | 'descanso'; iniciadaEm: string; duracaoSegundos: number; decorridoSegundos: number }

export interface EstadoDto {
  personagem: PersonagemDto
  atributos: AtributoDto[]
  missoesHoje: MissaoDto[]
  chefe: ChefeDto
  conquistas: ConquistaDto[]
  loja: ItemLojaDto[]
  compras: CompraDto[]
  focoAtivo: FocoDto | null
  agoraServidor: string
}

// ---------- Fase 2 — Corpo ----------

export interface PerfilCorporalDto {
  pesoKg: number; alturaCm: number; idade: number
  sexo: 'm' | 'f'; atividade: string; objetivo: string
}

export interface MetasDto { calorias: number; proteinaG: number; gorduraG: number; carboG: number; fibrasG: number; aguaMl: number }
export interface RefeicaoDto { nome: string; kcal: number; sugestao: string }

export interface ExercicioDto {
  id: string; nome: string; emoji: string; grupo: string; basico: boolean
  melhorRm1: number | null; melhorMarca: string | null; melhorEm: string | null
}

export interface RegistroCargaDto { id: number; exercicioId: string; cargaKg: number; reps: number; rm1: number; pr: boolean; data: string }
export interface FichaDiaDto { nome: string; exercicios: string[] }
export interface FichaDto { id: string; nome: string; objetivo: string; frequencia: string; dias: FichaDiaDto[] }
export interface FaixaCardioDto { faixaKm: number; melhorPaceSegKm: number | null; melhorEm: string | null }
export interface RegistroCardioDto { id: number; distanciaKm: number; duracaoMin: number; paceSegKm: number; faixaKm: number | null; pr: boolean; data: string }
export interface AmigoDto { amizadeId: number; nome: string; situacao: 'pendenteEnviado' | 'pendenteRecebido' | 'aceita' }
export interface RankingEntradaDto { nome: string; valor: number; relativo: number | null; ehVoce: boolean }
export interface RankingDto { chave: string; titulo: string; amigos: RankingEntradaDto[]; geral: RankingEntradaDto[] }

export interface CorpoDto {
  perfil: PerfilCorporalDto | null
  metas: MetasDto | null
  plano: RefeicaoDto[]
  exercicios: ExercicioDto[]
  cargas: RegistroCargaDto[]
  fichas: FichaDto[]
  faixasCardio: FaixaCardioDto[]
  cardios: RegistroCardioDto[]
  kmMes: number
  conselhos: string[]
  codigoAmigo: string
  rankingOptIn: boolean
  avisoSaudeAceito: boolean
  amigos: AmigoDto[]
  rankingsForca: RankingDto[]
  rankingsCardio: RankingDto[]
}

// ---------- Fase 3 — Bolso ----------

export interface PerfilFinanceiroDto {
  rendaMensal: number; despesasFixas: number; despesasVariaveis: number; orcamentoRecompensa: number
}

export interface DiagnosticoDto {
  mesesReserva: number; reservaMeta: number; reservaFaltante: number
  taxaPoupancaPct: number; metaPoupancaPct: number; pctNecessidades: number; pctDesejos: number
  score: number; nivel: string; metaDoMesBatida: boolean
}

export interface DividaDto { id: number; nome: string; valorAtual: number; jurosPctMes: number; quitada: boolean }
export interface AporteDto { id: number; valor: number; data: string }
export interface ConversaoDto { convertidoMesLibras: number; tetoMesLibras: number; liberadoTotalLibras: number }

export interface FinancasDto {
  perfil: PerfilFinanceiroDto | null
  diagnostico: DiagnosticoDto | null
  dividas: DividaDto[]
  aportes: AporteDto[]
  aportesDoMes: number
  conversao: ConversaoDto
  conselhos: string[]
  avisoAceito: boolean
}

// ---------- Fase 3 — Mente ----------

export interface TrilhaDto { id: string; nome: string; emoji: string; marcos: string[]; jaAdicionada: boolean }
export interface MarcoDto { indice: number; nome: string; concluido: boolean }

export interface HabilidadeDto {
  id: number; trilhaId: string | null; nome: string; emoji: string
  horasFoco: number; marcos: MarcoDto[]
}

export interface LivroDto { id: number; titulo: string; habilidadeId: number | null; concluido: boolean }

export interface MenteDto {
  habilidades: HabilidadeDto[]
  trilhas: TrilhaDto[]
  livros: LivroDto[]
  livrosConcluidos: number
  marcosConcluidos: number
  interacaoHoje: boolean
  diasSociais30: number
  conselhos: string[]
}

export interface EventoDto {
  tipo: string; titulo?: string; level?: number; nome?: string; emoji?: string; recompensa?: string
}

export interface AcaoResp {
  estado: EstadoDto; eventos: EventoDto[]
  corpo: CorpoDto | null; financas: FinancasDto | null; mente: MenteDto | null
}

// ---------- Sessão ----------

const CHAVE_TOKEN = 'lifesystem-token'

export const sessao = {
  token: () => localStorage.getItem(CHAVE_TOKEN),
  entrar: (token: string) => localStorage.setItem(CHAVE_TOKEN, token),
  sair: () => localStorage.removeItem(CHAVE_TOKEN),
}

export class ApiErro extends Error {
  status: number
  constructor(mensagem: string, status: number) {
    super(mensagem)
    this.status = status
  }
}

async function requisicao<T>(caminho: string, opcoes: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  const token = sessao.token()
  if (token) headers.Authorization = `Bearer ${token}`

  const resp = await fetch(`${BASE}${caminho}`, { ...opcoes, headers })
  if (resp.status === 401) {
    sessao.sair()
    throw new ApiErro('Sessão expirada — entre de novo', 401)
  }
  if (!resp.ok) {
    const corpo = await resp.json().catch(() => null)
    throw new ApiErro(corpo?.erro ?? `Erro ${resp.status}`, resp.status)
  }
  return resp.json()
}

// ---------- Auth ----------

export interface AuthResp { token: string; email: string; nomePersonagem: string }

export const auth = {
  registrar: (email: string, senha: string, nomePersonagem: string) =>
    requisicao<AuthResp>('/api/auth/registrar', { method: 'POST', body: JSON.stringify({ email, senha, nomePersonagem }) }),
  login: (email: string, senha: string) =>
    requisicao<AuthResp>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, senha }) }),
}

// ---------- Ações de jogo ----------

export const jogo = {
  estado: () => requisicao<AcaoResp>('/api/jogo/estado'),
  concluirMissao: (id: string) =>
    requisicao<AcaoResp>(`/api/jogo/missoes/${id}/concluir`, { method: 'POST' }),
  marcarCheck: (id: string, indice: number, marcado: boolean) =>
    requisicao<AcaoResp>(`/api/jogo/missoes/${id}/checklist`, { method: 'POST', body: JSON.stringify({ indice, marcado }) }),
  iniciarFoco: (tipo: 'foco' | 'descanso', habilidadeId?: number | null) =>
    requisicao<AcaoResp>('/api/jogo/foco/iniciar', { method: 'POST', body: JSON.stringify({ tipo, habilidadeId: habilidadeId ?? null }) }),
  encerrarFoco: (abandonar: boolean) =>
    requisicao<AcaoResp>('/api/jogo/foco/encerrar', { method: 'POST', body: JSON.stringify({ abandonar }) }),
  escolherClasse: (classe: string) =>
    requisicao<AcaoResp>('/api/jogo/classe', { method: 'POST', body: JSON.stringify({ classe }) }),
  definirRecompensa: (texto: string) =>
    requisicao<AcaoResp>('/api/jogo/chefe/recompensa', { method: 'POST', body: JSON.stringify({ texto }) }),
  definirEconomias: (valor: number) =>
    requisicao<AcaoResp>('/api/jogo/economias', { method: 'PUT', body: JSON.stringify({ valor }) }),
  adicionarItemLoja: (nome: string, preco: number) =>
    requisicao<AcaoResp>('/api/jogo/loja/itens', { method: 'POST', body: JSON.stringify({ nome, preco }) }),
  comprarItem: (itemId: number) =>
    requisicao<AcaoResp>(`/api/jogo/loja/itens/${itemId}/comprar`, { method: 'POST' }),

  // ---------- Fase 2 — Corpo ----------
  corpo: () => requisicao<AcaoResp>('/api/jogo/corpo'),
  definirPerfil: (perfil: PerfilCorporalDto) =>
    requisicao<AcaoResp>('/api/jogo/corpo/perfil', { method: 'PUT', body: JSON.stringify(perfil) }),
  aceitarAvisoSaude: () =>
    requisicao<AcaoResp>('/api/jogo/corpo/aviso', { method: 'POST' }),
  registrarCarga: (exercicioId: string, cargaKg: number, reps: number) =>
    requisicao<AcaoResp>('/api/jogo/corpo/carga', { method: 'POST', body: JSON.stringify({ exercicioId, cargaKg, reps }) }),
  registrarCardio: (distanciaKm: number, duracaoMin: number) =>
    requisicao<AcaoResp>('/api/jogo/corpo/cardio', { method: 'POST', body: JSON.stringify({ distanciaKm, duracaoMin }) }),
  definirRankingOptIn: (optIn: boolean) =>
    requisicao<AcaoResp>('/api/jogo/corpo/ranking', { method: 'PUT', body: JSON.stringify({ optIn }) }),
  adicionarAmigo: (codigo: string) =>
    requisicao<AcaoResp>('/api/jogo/amigos', { method: 'POST', body: JSON.stringify({ codigo }) }),
  responderAmizade: (amizadeId: number, aceitar: boolean) =>
    requisicao<AcaoResp>(`/api/jogo/amigos/${amizadeId}/responder`, { method: 'POST', body: JSON.stringify({ aceitar }) }),

  // ---------- Fase 3 — Bolso ----------
  financas: () => requisicao<AcaoResp>('/api/jogo/financas'),
  definirPerfilFinanceiro: (perfil: PerfilFinanceiroDto) =>
    requisicao<AcaoResp>('/api/jogo/financas/perfil', { method: 'PUT', body: JSON.stringify(perfil) }),
  aceitarAvisoFinancas: () =>
    requisicao<AcaoResp>('/api/jogo/financas/aviso', { method: 'POST' }),
  registrarAporte: (valor: number) =>
    requisicao<AcaoResp>('/api/jogo/financas/aporte', { method: 'POST', body: JSON.stringify({ valor }) }),
  criarDivida: (nome: string, valor: number, jurosPctMes: number) =>
    requisicao<AcaoResp>('/api/jogo/financas/dividas', { method: 'POST', body: JSON.stringify({ nome, valor, jurosPctMes }) }),
  pagarDivida: (dividaId: number, valor: number) =>
    requisicao<AcaoResp>(`/api/jogo/financas/dividas/${dividaId}/pagar`, { method: 'POST', body: JSON.stringify({ valor }) }),
  converterMoedas: (moedas: number) =>
    requisicao<AcaoResp>('/api/jogo/financas/converter', { method: 'POST', body: JSON.stringify({ moedas }) }),

  // ---------- Fase 3 — Mente ----------
  mente: () => requisicao<AcaoResp>('/api/jogo/mente'),
  criarHabilidade: (trilhaId: string | null, nome: string | null) =>
    requisicao<AcaoResp>('/api/jogo/mente/habilidades', { method: 'POST', body: JSON.stringify({ trilhaId, nome }) }),
  concluirMarco: (habilidadeId: number, indice: number) =>
    requisicao<AcaoResp>(`/api/jogo/mente/habilidades/${habilidadeId}/marcos/${indice}`, { method: 'POST' }),
  criarLivro: (titulo: string, habilidadeId: number | null) =>
    requisicao<AcaoResp>('/api/jogo/mente/livros', { method: 'POST', body: JSON.stringify({ titulo, habilidadeId }) }),
  concluirLivro: (livroId: number) =>
    requisicao<AcaoResp>(`/api/jogo/mente/livros/${livroId}/concluir`, { method: 'POST' }),
  registrarSocial: () =>
    requisicao<AcaoResp>('/api/jogo/mente/social', { method: 'POST' }),
}

/** Pace em s/km → "5:32" */
export const fmtPace = (segKm: number) => `${Math.floor(segKm / 60)}:${String(segKm % 60).padStart(2, '0')}`

export const fmt = (n: number) => Math.round(n).toLocaleString('pt-BR')
