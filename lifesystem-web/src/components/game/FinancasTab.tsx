import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { fmt } from '@/lib/api'
import type { FinancasDto, PerfilFinanceiroDto } from '@/lib/api'

interface Props {
  financas: FinancasDto | null
  moedas: number
  economias: number
  onCarregar: () => void
  onAviso: () => void
  onPerfil: (p: PerfilFinanceiroDto) => void
  onAporte: (valor: number) => void
  onDivida: (nome: string, valor: number, jurosPctMes: number) => void
  onPagar: (dividaId: number, valor: number) => void
  onConverter: (moedas: number) => void
}

const CORES_NIVEL: Record<string, string> = {
  E: 'text-zinc-400', D: 'text-sky-400', C: 'text-emerald-400',
  B: 'text-violet-400', A: 'text-amber-400', S: 'text-red-400',
}

export function FinancasTab(props: Props) {
  const { financas, onCarregar, onAviso } = props

  useEffect(() => { if (!financas) onCarregar() }, [financas, onCarregar])

  if (!financas) return <p className="py-10 text-center text-sm text-muted-foreground">Carregando o módulo Bolso…</p>

  // Disclaimer obrigatório no primeiro uso (PRD 4.5)
  if (!financas.avisoAceito) {
    return (
      <section className="mx-auto max-w-xl border border-amber-400/40 bg-card p-6 text-center">
        <p className="text-3xl">⚠️</p>
        <h2 className="mt-2 font-display text-lg uppercase tracking-widest">Antes de começar</h2>
        <p className="mt-3 text-sm text-muted-foreground">
          Os conselhos financeiros do Life System são <strong>educacionais</strong>, calculados por regras
          consagradas (reserva de 6 meses, 50/30/20, método avalanche) — eles <strong>não substituem</strong> um
          consultor financeiro e nunca indicam investimentos específicos. Decisões de dinheiro são suas.
        </p>
        <Button className="mt-5 font-display uppercase tracking-widest" onClick={onAviso}>
          Entendi, vamos organizar
        </Button>
      </section>
    )
  }

  return (
    <div className="space-y-4">
      {financas.conselhos.length > 0 && (
        <section className="border border-cyan-400/30 bg-card p-4">
          <h3 className="font-display text-xs uppercase tracking-[0.25em] text-cyan-400">Conselheiro</h3>
          <ul className="mt-2 space-y-1">
            {financas.conselhos.map((c, i) => (
              <li key={i} className="text-sm text-muted-foreground">▸ {c}</li>
            ))}
          </ul>
        </section>
      )}

      <div className="grid gap-4 lg:grid-cols-[1fr_1.2fr]">
        <div className="space-y-4">
          <SecaoPerfil {...props} financas={financas} />
          <SecaoConversao {...props} financas={financas} />
        </div>
        <div className="space-y-4">
          <SecaoDiagnostico {...props} financas={financas} />
          <SecaoAportes {...props} financas={financas} />
          <SecaoDividas {...props} financas={financas} />
        </div>
      </div>
    </div>
  )
}

// ---------- Perfil (PRD 4.1) ----------

function SecaoPerfil({ financas, onPerfil }: Props & { financas: FinancasDto }) {
  const p = financas.perfil
  const [renda, setRenda] = useState(p ? String(p.rendaMensal) : '')
  const [fixas, setFixas] = useState(p ? String(p.despesasFixas) : '')
  const [variaveis, setVariaveis] = useState(p ? String(p.despesasVariaveis) : '')
  const [orcamento, setOrcamento] = useState(p ? String(p.orcamentoRecompensa) : '')

  const num = (s: string) => Number(s.replace(',', '.'))

  return (
    <section className="border border-border bg-card p-4">
      <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Perfil financeiro</h3>
      <form
        className="mt-3 space-y-2"
        onSubmit={e => {
          e.preventDefault()
          const r = num(renda)
          if (r >= 0)
            onPerfil({
              rendaMensal: r,
              despesasFixas: num(fixas) || 0,
              despesasVariaveis: num(variaveis) || 0,
              orcamentoRecompensa: num(orcamento) || 0,
            })
        }}
      >
        <Input value={renda} onChange={e => setRenda(e.target.value)} placeholder="renda mensal (£)" aria-label="Renda mensal" inputMode="decimal" className="font-mono" />
        <div className="flex gap-2">
          <Input value={fixas} onChange={e => setFixas(e.target.value)} placeholder="despesas fixas (£)" aria-label="Despesas fixas" inputMode="decimal" className="font-mono" />
          <Input value={variaveis} onChange={e => setVariaveis(e.target.value)} placeholder="variáveis (£)" aria-label="Despesas variáveis" inputMode="decimal" className="font-mono" />
        </div>
        <Input value={orcamento} onChange={e => setOrcamento(e.target.value)} placeholder="orçamento de recompensa mensal (£)" aria-label="Orçamento de recompensa" inputMode="decimal" className="font-mono" />
        <Button type="submit" className="w-full font-display uppercase tracking-widest">
          {p ? 'Atualizar diagnóstico' : 'Gerar diagnóstico'}
        </Button>
      </form>
      <p className="mt-2 text-xs text-muted-foreground">
        O orçamento de recompensa sai da fatia “desejos” do 50/30/20 — é ele que habilita a conversão de moedas.
      </p>
    </section>
  )
}

// ---------- Diagnóstico gamificado (PRD 4.1) ----------

function SecaoDiagnostico({ financas, economias }: Props & { financas: FinancasDto }) {
  const d = financas.diagnostico
  if (!d) {
    return (
      <section className="border border-border bg-card p-4">
        <p className="text-sm text-muted-foreground">Preencha o perfil ao lado para gerar seu diagnóstico de saúde financeira.</p>
      </section>
    )
  }

  return (
    <section className="border border-border bg-card p-4">
      <div className="flex items-center justify-between">
        <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Diagnóstico</h3>
        <p className="font-display text-sm">
          Nível Financeiro{' '}
          <span className={`text-3xl font-bold ${CORES_NIVEL[d.nivel]}`} data-testid="nivel-financeiro">{d.nivel}</span>
        </p>
      </div>

      <div className="mt-3 grid grid-cols-2 gap-2 text-center sm:grid-cols-4">
        <div className="border border-border bg-secondary/40 px-2 py-3">
          <p className="text-lg">🛡️</p>
          <p className="font-mono text-sm text-amber-400">{d.mesesReserva.toLocaleString('pt-BR')} meses</p>
          <p className="text-xs text-muted-foreground">reserva (meta 6)</p>
        </div>
        <div className="border border-border bg-secondary/40 px-2 py-3">
          <p className="text-lg">🐷</p>
          <p className="font-mono text-sm text-amber-400">{d.taxaPoupancaPct.toLocaleString('pt-BR')}%</p>
          <p className="text-xs text-muted-foreground">poupança no mês</p>
        </div>
        <div className="border border-border bg-secondary/40 px-2 py-3">
          <p className="text-lg">🏠</p>
          <p className="font-mono text-sm text-amber-400">{d.pctNecessidades.toLocaleString('pt-BR')}%</p>
          <p className="text-xs text-muted-foreground">necessidades (50%)</p>
        </div>
        <div className="border border-border bg-secondary/40 px-2 py-3">
          <p className="text-lg">🎁</p>
          <p className="font-mono text-sm text-amber-400">{d.pctDesejos.toLocaleString('pt-BR')}%</p>
          <p className="text-xs text-muted-foreground">desejos (30%)</p>
        </div>
      </div>

      <div className="mt-3">
        <div className="flex justify-between font-mono text-xs text-muted-foreground">
          <span>Missão mensal: poupar {d.metaPoupancaPct}% da renda {d.metaDoMesBatida && '· batida ✓ (+300 XP)'}</span>
          <span>score {d.score}/100</span>
        </div>
        <div className="mt-1 h-2 w-full bg-secondary">
          <div
            className={`h-full transition-all ${d.metaDoMesBatida ? 'bg-gradient-to-r from-emerald-500 to-amber-400' : 'bg-gradient-to-r from-cyan-500 to-amber-400'}`}
            style={{ width: `${Math.min(100, (d.taxaPoupancaPct / d.metaPoupancaPct) * 100)}%` }}
          />
        </div>
      </div>

      <p className="mt-3 border-t border-border pt-2 font-mono text-xs text-muted-foreground">
        💰 Total economizado: £{fmt(economias)} · o score alimenta seu atributo 💰 Finanças
      </p>
    </section>
  )
}

// ---------- Aportes ----------

function SecaoAportes({ financas, onAporte }: Props & { financas: FinancasDto }) {
  const [valor, setValor] = useState('')

  return (
    <section className="border border-border bg-card p-4">
      <div className="flex items-baseline justify-between">
        <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Aportes</h3>
        <p className="font-mono text-sm text-emerald-400">£{fmt(financas.aportesDoMes)} este mês</p>
      </div>
      <form
        className="mt-3 flex gap-2"
        onSubmit={e => {
          e.preventDefault()
          const v = Number(valor.replace(',', '.'))
          if (v !== 0 && !Number.isNaN(v)) { onAporte(v); setValor('') }
        }}
      >
        <Input value={valor} onChange={e => setValor(e.target.value)} placeholder="valor (£) — negativo retira" aria-label="Valor do aporte" inputMode="decimal" className="font-mono" />
        <Button type="submit" className="font-display uppercase tracking-widest">Aportar</Button>
      </form>
      {financas.aportes.length > 0 && (
        <ul className="mt-3 max-h-32 space-y-1 overflow-y-auto">
          {financas.aportes.map(a => (
            <li key={a.id} className="flex justify-between font-mono text-sm">
              <span className="text-muted-foreground">{formatarData(a.data)}</span>
              <span className={a.valor >= 0 ? 'text-emerald-400' : 'text-red-400'}>
                {a.valor >= 0 ? '+' : '−'}£{fmt(Math.abs(a.valor))}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

// ---------- Dívidas (método avalanche, PRD 4.1) ----------

function SecaoDividas({ financas, onDivida, onPagar }: Props & { financas: FinancasDto }) {
  const [nome, setNome] = useState('')
  const [valor, setValor] = useState('')
  const [juros, setJuros] = useState('')
  const [pagando, setPagando] = useState<Record<number, string>>({})

  const abertas = financas.dividas.filter(d => !d.quitada)
  const quitadas = financas.dividas.filter(d => d.quitada)

  return (
    <section className="border border-border bg-card p-4">
      <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Dívidas — chefes do bolso</h3>
      <form
        className="mt-3 flex flex-wrap gap-2"
        onSubmit={e => {
          e.preventDefault()
          const v = Number(valor.replace(',', '.'))
          const j = Number(juros.replace(',', '.'))
          if (nome.trim() && v > 0 && j >= 0) {
            onDivida(nome.trim(), v, j)
            setNome(''); setValor(''); setJuros('')
          }
        }}
      >
        <Input value={nome} onChange={e => setNome(e.target.value)} placeholder="nome (ex.: Cartão)" aria-label="Nome da dívida" className="min-w-32 flex-1" />
        <Input value={valor} onChange={e => setValor(e.target.value)} placeholder="valor (£)" aria-label="Valor da dívida" inputMode="decimal" className="w-28 font-mono" />
        <Input value={juros} onChange={e => setJuros(e.target.value)} placeholder="juros %/mês" aria-label="Juros ao mês" inputMode="decimal" className="w-28 font-mono" />
        <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Adicionar</Button>
      </form>

      <ul className="mt-3 space-y-2">
        {abertas.length === 0 && quitadas.length === 0 && (
          <li className="text-sm text-muted-foreground">Nenhuma dívida — que continue assim. 🛡️</li>
        )}
        {abertas.map((d, i) => (
          <li key={d.id} className="border border-red-400/30 bg-secondary/40 px-3 py-2">
            <div className="flex items-baseline justify-between">
              <p className="text-sm">
                👹 {d.nome}
                {i === 0 && abertas.length > 1 && <span className="ml-2 text-xs text-red-400">← avalanche: esta primeiro</span>}
              </p>
              <p className="font-mono text-sm text-red-400">£{fmt(d.valorAtual)} · {d.jurosPctMes.toLocaleString('pt-BR')}%/mês</p>
            </div>
            <form
              className="mt-2 flex gap-2"
              onSubmit={e => {
                e.preventDefault()
                const v = Number((pagando[d.id] ?? '').replace(',', '.'))
                if (v > 0) { onPagar(d.id, v); setPagando(prev => ({ ...prev, [d.id]: '' })) }
              }}
            >
              <Input
                value={pagando[d.id] ?? ''}
                onChange={e => setPagando(prev => ({ ...prev, [d.id]: e.target.value }))}
                placeholder="pagar (£)"
                aria-label={`Pagar ${d.nome}`}
                inputMode="decimal"
                className="h-8 w-32 font-mono"
              />
              <Button type="submit" size="sm" className="h-8 font-display text-xs uppercase tracking-widest">⚔️ Atacar</Button>
            </form>
          </li>
        ))}
        {quitadas.map(d => (
          <li key={d.id} className="flex justify-between border border-emerald-400/30 bg-secondary/20 px-3 py-2 text-sm text-muted-foreground">
            <span>⛓️ {d.nome}</span>
            <span className="text-emerald-400">quitada ✓</span>
          </li>
        ))}
      </ul>
      <p className="mt-2 text-xs text-muted-foreground">
        Quitar uma dívida vale como chefe derrotado: +1.000 XP e +200 🪙.
      </p>
    </section>
  )
}

// ---------- Conversão de moedas (PRD 3.8) ----------

function SecaoConversao({ financas, moedas, onConverter }: Props & { financas: FinancasDto }) {
  const [qtde, setQtde] = useState('')
  const c = financas.conversao
  const temOrcamento = c.tetoMesLibras > 0
  const restante = Math.max(0, c.tetoMesLibras - c.convertidoMesLibras)

  return (
    <section className="border border-amber-400/30 bg-card p-4">
      <h3 className="font-display text-sm uppercase tracking-[0.25em] text-amber-400">Converter moedas → £</h3>
      <p className="mt-2 text-sm text-muted-foreground">
        10 🪙 = £1. As moedas liberam o <strong>seu próprio</strong> orçamento de recompensa — dinheiro para
        gastar sem culpa, conquistado com disciplina.
      </p>

      {temOrcamento ? (
        <>
          <div className="mt-3">
            <div className="flex justify-between font-mono text-xs text-muted-foreground">
              <span>convertido no mês</span>
              <span>£{fmt(c.convertidoMesLibras)}/£{fmt(c.tetoMesLibras)}</span>
            </div>
            <div className="mt-1 h-2 w-full bg-secondary">
              <div className="h-full bg-gradient-to-r from-amber-500 to-amber-300 transition-all" style={{ width: `${Math.min(100, (c.convertidoMesLibras / c.tetoMesLibras) * 100)}%` }} />
            </div>
          </div>
          <form
            className="mt-3 flex gap-2"
            onSubmit={e => {
              e.preventDefault()
              const m = Math.floor(Number(qtde))
              if (m >= 10) { onConverter(m - (m % 10)); setQtde('') }
            }}
          >
            <Input value={qtde} onChange={e => setQtde(e.target.value)} placeholder={`moedas (você tem ${fmt(moedas)})`} aria-label="Moedas a converter" inputMode="numeric" className="font-mono" />
            <Button type="submit" className="font-display uppercase tracking-widest" disabled={restante <= 0}>
              Converter
            </Button>
          </form>
          <p className="mt-2 font-mono text-xs text-muted-foreground">
            💷 Liberado no total: £{fmt(c.liberadoTotalLibras)} · restam £{fmt(restante)} no teto do mês
          </p>
        </>
      ) : (
        <p className="mt-3 border border-border bg-secondary/40 px-3 py-2 text-sm text-muted-foreground">
          Defina um <strong>orçamento de recompensa</strong> no perfil acima para desbloquear a conversão —
          só converte quem organizou as finanças. 😉
        </p>
      )}
    </section>
  )
}

function formatarData(iso: string) {
  const [, m, d] = iso.split('-')
  return `${d}/${m}`
}
