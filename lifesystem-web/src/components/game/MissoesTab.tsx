import { Checkbox } from '@/components/ui/checkbox'
import { Button } from '@/components/ui/button'
import { fmt } from '@/lib/api'
import type { EstadoDto } from '@/lib/api'

interface Props {
  estado: EstadoDto
  onConcluir: (id: string) => void
  onCheck: (id: string, indice: number, marcado: boolean) => void
  onAbrirFoco: () => void
}

export function MissoesTab({ estado, onConcluir, onCheck, onAbrirFoco }: Props) {
  const { personagem, missoesHoje } = estado
  const concluidas = missoesHoje.filter(m => m.concluida).length
  const diaPerfeito = concluidas === missoesHoje.length

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2 border border-border bg-card px-4 py-3">
        <p className="font-display text-sm uppercase tracking-widest text-muted-foreground">
          Missões de hoje — <span className="text-foreground">{concluidas}/{missoesHoje.length}</span>
        </p>
        <p className="font-mono text-sm text-amber-400">
          🔥 Streak {personagem.streakDias}d → XP ×{personagem.multiplicadorStreak.toFixed(2).replace('.', ',')}{' '}
          <span className="text-muted-foreground">(máx ×1,40)</span>
        </p>
      </div>

      {diaPerfeito && (
        <div className="border border-amber-500/60 bg-amber-500/10 px-4 py-3 font-display uppercase tracking-widest text-amber-300">
          ✨ Dia perfeito! +100 XP · +20 🪙
        </div>
      )}

      <div className="grid gap-3 md:grid-cols-2">
        {missoesHoje.map(m => (
          <div
            key={m.id}
            data-testid={`missao-${m.id}`}
            className={`border px-4 py-3 transition-colors ${m.concluida ? 'border-amber-500/60 bg-amber-500/5' : 'border-border bg-card'}`}
          >
            <div className="flex items-start justify-between gap-2">
              <div>
                <h3 className="font-display text-lg font-semibold">
                  {m.emoji} {m.nome} {m.concluida && <span className="text-amber-400">✓</span>}
                </h3>
                <p className="text-sm text-muted-foreground">{m.requisito}</p>
              </div>
              <div className="text-right font-mono text-xs leading-5 text-muted-foreground">
                <div className="text-emerald-400">+{fmt(m.xpFinal)} XP</div>
                <div className="text-amber-400">+{fmt(m.moedasFinal)} 🪙</div>
                {m.danoChefe > 0 && <div className="text-red-400">−{m.danoChefe} HP chefe</div>}
                {m.bonusClasse && <div className="text-cyan-400">classe +20%</div>}
              </div>
            </div>

            {m.checklist ? (
              <ul className="mt-3 space-y-2">
                {m.checklist.map((item, i) => (
                  <li key={item} className="flex items-center gap-2">
                    <Checkbox
                      id={`${m.id}-${i}`}
                      checked={m.checks[i] ?? false}
                      disabled={m.concluida}
                      onCheckedChange={v => onCheck(m.id, i, v === true)}
                    />
                    <label htmlFor={`${m.id}-${i}`} className={`text-sm ${m.checks[i] ? 'text-muted-foreground line-through' : ''}`}>
                      {item}
                    </label>
                  </li>
                ))}
              </ul>
            ) : m.minutosNecessarios ? (
              <div className="mt-3 space-y-2">
                <div className="flex items-center justify-between font-mono text-xs text-muted-foreground">
                  <span>⏱️ {m.progressoMinutos}/{m.minutosNecessarios} min</span>
                  {!m.concluida && <span>via Modo Foco ou registro manual</span>}
                </div>
                <div className="h-1.5 w-full bg-secondary">
                  <div
                    className="h-full bg-gradient-to-r from-cyan-500 to-amber-400 transition-all"
                    style={{ width: `${Math.min(100, (m.progressoMinutos / m.minutosNecessarios) * 100)}%` }}
                  />
                </div>
                {!m.concluida && (
                  <div className="flex gap-2">
                    <Button className="flex-1 font-display uppercase tracking-widest" variant="secondary" onClick={onAbrirFoco}>
                      ⏱️ Modo Foco
                    </Button>
                    <Button className="flex-1 font-display uppercase tracking-widest" onClick={() => onConcluir(m.id)}>
                      Registrar 2h
                    </Button>
                  </div>
                )}
              </div>
            ) : (
              <Button
                className="mt-3 w-full font-display uppercase tracking-widest"
                variant={m.concluida ? 'secondary' : 'default'}
                disabled={m.concluida}
                onClick={() => onConcluir(m.id)}
              >
                {m.concluida ? 'Concluída' : 'Concluir missão'}
              </Button>
            )}
          </div>
        ))}
      </div>

      <p className="text-xs text-muted-foreground">
        Toda missão concluída também alimenta ⚡ Disciplina. Bônus de dia perfeito ao completar as 6.
      </p>
    </div>
  )
}
