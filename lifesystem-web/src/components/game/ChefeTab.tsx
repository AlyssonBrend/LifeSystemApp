import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { fmt } from '@/lib/api'
import type { EstadoDto } from '@/lib/api'

interface Props {
  estado: EstadoDto
  onRecompensa: (texto: string) => void
}

export function ChefeTab({ estado, onRecompensa }: Props) {
  const { chefe, missoesHoje, personagem } = estado
  const [recompensa, setRecompensa] = useState(chefe.recompensaCaixa)
  const pct = (chefe.hpAtual / chefe.hpMax) * 100
  const vencido = chefe.status === 'vencida'

  return (
    <div className="grid gap-4 lg:grid-cols-[1.4fr_1fr]">
      <section className="border border-border bg-card p-5" data-testid="chefe-card">
        <p className="font-display text-xs uppercase tracking-[0.3em] text-red-400">
          Chefe da semana {chefe.enfurecido && '· enfurecido +10% HP'}
        </p>
        <div className="mt-2 flex items-center gap-4">
          <span className={`text-6xl ${vencido ? 'opacity-40 grayscale' : ''}`} aria-hidden>{chefe.emoji}</span>
          <div>
            <h2 className="font-display text-3xl font-bold uppercase tracking-wider">{chefe.nome}</h2>
            <p className="font-mono text-sm text-muted-foreground">
              {vencido
                ? <span className="text-amber-400">DERROTADO 👑</span>
                : <>HP <span className="text-red-400">{fmt(chefe.hpAtual)}</span> / {fmt(chefe.hpMax)}</>}
            </p>
          </div>
        </div>

        <div className="mt-4 h-5 w-full border border-red-900/60 bg-secondary" role="progressbar" aria-label="HP do chefe" aria-valuenow={chefe.hpAtual} aria-valuemax={chefe.hpMax}>
          <div className="h-full bg-gradient-to-r from-red-700 to-red-500 transition-all duration-500" style={{ width: `${pct}%` }} />
        </div>

        {vencido ? (
          <p className="mt-4 text-sm text-amber-300">
            🎁 Caixa aberta: {chefe.recompensaCaixa || 'sem recompensa definida'} — aproveite sem culpa.
            O próximo chefe surge na segunda-feira.
          </p>
        ) : (
          <div className="mt-4">
            <p className="font-display text-xs uppercase tracking-[0.25em] text-muted-foreground">
              Ataques dele (o que ele quer que você faça)
            </p>
            <ul className="mt-1 flex flex-wrap gap-2">
              {chefe.ataques.map(a => (
                <li key={a} className="border border-red-900/50 bg-red-950/30 px-2 py-1 text-xs text-red-200">{a}</li>
              ))}
            </ul>
          </div>
        )}

        <p className="mt-4 border-t border-border pt-3 text-xs text-muted-foreground">
          Vitória: +1.000 XP · +200 🪙 · caixa de recompensa. Derrota não pune — mas ele volta enfurecido (+10% HP).
          Quebrar a streak cura o chefe em 100 HP. Chefes derrotados: <span className="text-red-400">{personagem.chefesDerrotados}</span>.
        </p>
      </section>

      <div className="space-y-4">
        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Dano por hábito</h3>
          <ul className="mt-2 space-y-1.5">
            {missoesHoje.filter(m => m.danoChefe > 0).map(m => (
              <li key={m.id} className="flex items-center justify-between font-mono text-sm">
                <span>{m.emoji} {m.nome}{m.concluida && <span className="ml-2 text-amber-400">✓</span>}</span>
                <span className="text-red-400">−{m.danoChefe} HP</span>
              </li>
            ))}
          </ul>
        </section>

        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">🎁 Caixa de recompensa</h3>
          <p className="mt-1 text-xs text-muted-foreground">Defina a recompensa real antes de derrotar o chefe:</p>
          <form
            className="mt-2 flex gap-2"
            onSubmit={e => {
              e.preventDefault()
              if (recompensa.trim()) onRecompensa(recompensa.trim())
            }}
          >
            <Input value={recompensa} onChange={e => setRecompensa(e.target.value)} placeholder="ex.: jantar fora" aria-label="Recompensa da caixa" />
            <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Definir</Button>
          </form>
          <p className="mt-2 text-sm">
            Atual: <span className="text-amber-300">{chefe.recompensaCaixa || '— nada definido —'}</span>
          </p>
        </section>
      </div>
    </div>
  )
}
