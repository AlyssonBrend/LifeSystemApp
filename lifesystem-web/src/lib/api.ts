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

export interface EventoDto {
  tipo: string; titulo?: string; level?: number; nome?: string; emoji?: string; recompensa?: string
}

export interface AcaoResp { estado: EstadoDto; eventos: EventoDto[] }

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
  iniciarFoco: (tipo: 'foco' | 'descanso') =>
    requisicao<AcaoResp>('/api/jogo/foco/iniciar', { method: 'POST', body: JSON.stringify({ tipo }) }),
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
}

export const fmt = (n: number) => Math.round(n).toLocaleString('pt-BR')
