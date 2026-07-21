import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import type { HabilidadeDto, MenteDto } from '@/lib/api'

interface Props {
  mente: MenteDto | null
  onCarregar: () => void
  onHabilidade: (trilhaId: string | null, nome: string | null) => void
  onMarco: (habilidadeId: number, indice: number) => void
  onLivro: (titulo: string, habilidadeId: number | null) => void
  onConcluirLivro: (livroId: number) => void
  onSocial: () => void
  onAbrirFoco: () => void
}

export function MenteTab(props: Props) {
  const { mente, onCarregar } = props

  useEffect(() => { if (!mente) onCarregar() }, [mente, onCarregar])

  if (!mente) return <p className="py-10 text-center text-sm text-muted-foreground">Carregando o módulo Mente…</p>

  return (
    <div className="space-y-4">
      {mente.conselhos.length > 0 && (
        <section className="border border-cyan-400/30 bg-card p-4">
          <h3 className="font-display text-xs uppercase tracking-[0.25em] text-cyan-400">Conselheiro</h3>
          <ul className="mt-2 space-y-1">
            {mente.conselhos.map((c, i) => (
              <li key={i} className="text-sm text-muted-foreground">▸ {c}</li>
            ))}
          </ul>
        </section>
      )}

      <div className="grid gap-4 lg:grid-cols-[1.2fr_1fr]">
        <SecaoArvore {...props} mente={mente} />
        <div className="space-y-4">
          <SecaoSocial {...props} mente={mente} />
          <SecaoLivros {...props} mente={mente} />
        </div>
      </div>
    </div>
  )
}

// ---------- Árvore de conhecimento (PRD 4.4) ----------

function SecaoArvore({ mente, onHabilidade, onMarco, onAbrirFoco }: Props & { mente: MenteDto }) {
  const [nova, setNova] = useState('')
  const trilhasDisponiveis = mente.trilhas.filter(t => !t.jaAdicionada)

  return (
    <div className="space-y-4">
      <section className="border border-border bg-card p-4">
        <div className="flex items-baseline justify-between">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Árvore de conhecimento</h3>
          <p className="font-mono text-xs text-muted-foreground">
            🌳 {mente.marcosConcluidos} marcos · 📚 {mente.livrosConcluidos} livros
          </p>
        </div>

        {mente.habilidades.length === 0 && (
          <p className="mt-3 text-sm text-muted-foreground">
            Nenhuma habilidade ainda — adicione uma trilha abaixo ou crie a sua. Livros e marcos alimentam o 📚 Conhecimento.
          </p>
        )}

        <div className="mt-3 space-y-3">
          {mente.habilidades.map(h => (
            <CartaoHabilidade key={h.id} habilidade={h} onMarco={onMarco} onAbrirFoco={onAbrirFoco} />
          ))}
        </div>
      </section>

      <section className="border border-border bg-card p-4">
        <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Adicionar habilidade</h3>
        {trilhasDisponiveis.length > 0 && (
          <>
            <p className="mt-2 text-xs text-muted-foreground">Trilhas do catálogo (marcos prontos):</p>
            <div className="mt-2 flex flex-wrap gap-2">
              {trilhasDisponiveis.map(t => (
                <Button key={t.id} size="sm" variant="secondary" className="text-xs" onClick={() => onHabilidade(t.id, null)}>
                  {t.emoji} {t.nome}
                </Button>
              ))}
            </div>
          </>
        )}
        <form
          className="mt-3 flex gap-2"
          onSubmit={e => {
            e.preventDefault()
            if (nova.trim()) { onHabilidade(null, nova.trim()); setNova('') }
          }}
        >
          <Input value={nova} onChange={e => setNova(e.target.value)} placeholder="ou crie a sua (ex.: Xadrez)" aria-label="Nova habilidade" />
          <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Criar</Button>
        </form>
        <p className="mt-2 text-xs text-muted-foreground">
          Habilidades próprias destravam marcos por horas de foco: 10h → 25h → 50h → 100h.
        </p>
      </section>
    </div>
  )
}

function CartaoHabilidade({ habilidade: h, onMarco, onAbrirFoco }: {
  habilidade: HabilidadeDto
  onMarco: (habilidadeId: number, indice: number) => void
  onAbrirFoco: () => void
}) {
  const feitos = h.marcos.filter(m => m.concluido).length
  const pct = h.marcos.length > 0 ? (feitos / h.marcos.length) * 100 : 0
  const ehTrilha = h.trilhaId !== null

  return (
    <details className="border border-border bg-secondary/40 px-3 py-2" data-testid={`habilidade-${h.id}`}>
      <summary className="cursor-pointer">
        <span className="text-sm">{h.emoji} {h.nome}</span>
        <span className="ml-2 font-mono text-xs text-muted-foreground">
          {feitos}/{h.marcos.length} marcos · {h.horasFoco.toLocaleString('pt-BR')}h de foco
        </span>
        <div className="mt-1.5 h-1.5 w-full bg-secondary">
          <div className="h-full bg-gradient-to-r from-cyan-500 to-emerald-400 transition-all" style={{ width: `${pct}%` }} />
        </div>
      </summary>
      <ul className="mt-2 space-y-1.5">
        {h.marcos.map(m => (
          <li key={m.indice} className="flex items-center justify-between text-sm">
            <span className={m.concluido ? 'text-emerald-400' : 'text-muted-foreground'}>
              {m.concluido ? '✅' : '⬜'} {m.nome}
            </span>
            {ehTrilha && !m.concluido && (
              <Button size="sm" variant="secondary" className="h-6 px-2 text-xs" onClick={() => onMarco(h.id, m.indice)}>
                Concluir (+50 XP)
              </Button>
            )}
          </li>
        ))}
      </ul>
      <button className="mt-2 text-xs text-cyan-400 underline-offset-2 hover:underline" onClick={onAbrirFoco}>
        ⏱️ Estudar com o Modo Foco (credita as horas aqui)
      </button>
    </details>
  )
}

// ---------- Livros ----------

function SecaoLivros({ mente, onLivro, onConcluirLivro }: Props & { mente: MenteDto }) {
  const [titulo, setTitulo] = useState('')
  const [habilidadeId, setHabilidadeId] = useState<string>('nenhuma')

  const lendo = mente.livros.filter(l => !l.concluido)
  const lidos = mente.livros.filter(l => l.concluido)

  return (
    <section className="border border-border bg-card p-4">
      <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Lista de leitura</h3>
      <form
        className="mt-3 space-y-2"
        onSubmit={e => {
          e.preventDefault()
          if (titulo.trim()) {
            onLivro(titulo.trim(), habilidadeId === 'nenhuma' ? null : Number(habilidadeId))
            setTitulo('')
          }
        }}
      >
        <Input value={titulo} onChange={e => setTitulo(e.target.value)} placeholder="título do livro" aria-label="Título do livro" />
        <div className="flex gap-2">
          <Select value={habilidadeId} onValueChange={setHabilidadeId}>
            <SelectTrigger aria-label="Habilidade relacionada"><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="nenhuma">Sem habilidade ligada</SelectItem>
              {mente.habilidades.map(h => (
                <SelectItem key={h.id} value={String(h.id)}>{h.emoji} {h.nome}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Adicionar</Button>
        </div>
      </form>

      <ul className="mt-3 space-y-1.5">
        {lendo.length === 0 && lidos.length === 0 && (
          <li className="text-sm text-muted-foreground">Nenhum livro na lista — cada conclusão paga +100 XP.</li>
        )}
        {lendo.map(l => (
          <li key={l.id} className="flex items-center justify-between text-sm">
            <span>📖 {l.titulo}</span>
            <Button size="sm" variant="secondary" className="h-6 px-2 text-xs" onClick={() => onConcluirLivro(l.id)}>
              Terminei (+100 XP)
            </Button>
          </li>
        ))}
        {lidos.map(l => (
          <li key={l.id} className="flex items-center justify-between text-sm text-muted-foreground">
            <span>📕 {l.titulo}</span>
            <span className="text-xs text-emerald-400">lido ✓</span>
          </li>
        ))}
      </ul>
    </section>
  )
}

// ---------- Interação social (Carisma, PRD 3.1) ----------

function SecaoSocial({ mente, onSocial }: Props & { mente: MenteDto }) {
  return (
    <section className="border border-border bg-card p-4">
      <div className="flex items-baseline justify-between">
        <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">🤝 Interação social</h3>
        <p className="font-mono text-xs text-muted-foreground">{mente.diasSociais30}/30 dias</p>
      </div>
      <p className="mt-2 text-sm text-muted-foreground">
        Tempo de qualidade com alguém hoje? Um contato de verdade — pessoalmente, ligação, mensagem sincera.
        A constância alimenta o seu 🤝 Carisma.
      </p>
      {mente.interacaoHoje ? (
        <p className="mt-3 border border-emerald-400/30 bg-secondary/40 px-3 py-2 text-center text-sm text-emerald-400">
          ✅ Interação de hoje registrada
        </p>
      ) : (
        <Button className="mt-3 w-full font-display uppercase tracking-widest" onClick={onSocial} data-testid="registrar-social">
          Registrar interação de hoje (+20 XP)
        </Button>
      )}
    </section>
  )
}
