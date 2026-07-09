import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { fmt } from '@/lib/api'
import type { EstadoDto } from '@/lib/api'
import { CLASSES, NOMES_ATRIBUTOS } from '@/lib/classes'

interface Props {
  estado: EstadoDto
  onEconomias: (valor: number) => void
  onAbrirClasses: () => void
}

const CORES_FAIXA: Record<string, string> = {
  'Iniciante': 'text-zinc-400',
  'Casual': 'text-sky-400',
  'Intermediário': 'text-emerald-400',
  'Avançado': 'text-violet-400',
  'Elite': 'text-amber-400',
  'Lendário': 'text-red-400',
}

export function PainelTab({ estado, onEconomias, onAbrirClasses }: Props) {
  const { personagem, atributos, missoesHoje, conquistas } = estado
  const [economias, setEconomias] = useState(String(personagem.economias))
  const classe = CLASSES.find(c => c.id === personagem.classe)
  const concluidas = missoesHoje.filter(m => m.concluida).length

  return (
    <div className="space-y-6">
      <section>
        <h2 className="mb-3 font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">
          Atributos — espelho da vida real (0–100)
        </h2>
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
          {atributos.map(a => (
            <div key={a.id} data-testid={`attr-${a.id}`} className="border border-border bg-card px-3 py-2.5">
              <div className="flex items-baseline justify-between">
                <span className="text-sm">{a.emoji} {a.nome}</span>
                <span className="font-mono text-lg font-bold text-foreground">
                  {a.temDados ? a.valor : '—'}
                </span>
              </div>
              <div className="mt-1.5 h-1.5 w-full bg-secondary">
                <div className="h-full bg-gradient-to-r from-cyan-500 to-amber-400 transition-all" style={{ width: `${a.valor}%` }} />
              </div>
              <p className={`mt-1 text-[11px] uppercase tracking-wider ${a.temDados ? CORES_FAIXA[a.faixa] : 'text-zinc-500'}`}>
                {a.temDados ? a.faixa : 'dados na Fase 3'}
              </p>
            </div>
          ))}
        </div>
        <p className="mt-2 text-xs text-muted-foreground">
          Consistência dos últimos 30 dias gera os valores — e a inatividade os derruba. O jogo reflete a realidade.
        </p>
      </section>

      <div className="grid gap-4 lg:grid-cols-3">
        <section className="border border-border bg-card p-4">
          <h2 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Classe</h2>
          {classe ? (
            <>
              <p className="mt-2 font-display text-2xl">{classe.emoji} {classe.nome}</p>
              <p className="mt-1 text-sm italic text-muted-foreground">“{classe.lema}”</p>
              <p className="mt-2 text-sm">
                Primárias:{' '}
                {classe.primarias.map(p => (
                  <span key={p} className="mr-2 text-cyan-400">{NOMES_ATRIBUTOS[p]}</span>
                ))}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                {personagem.bonusClasseAtivo
                  ? 'Missões das primárias pagam +20% XP.'
                  : '⚠️ Bônus suspenso — um atributo do piso de manutenção está abaixo do mínimo.'}
              </p>
            </>
          ) : (
            <>
              <p className="mt-2 font-display text-2xl">🌾 Aldeão</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Um plebeu comum, antes do despertar. No level 5 você escolhe sua classe.
              </p>
              {personagem.podeEscolherClasse && (
                <Button className="mt-3 font-display uppercase tracking-widest" onClick={onAbrirClasses}>
                  Escolher classe
                </Button>
              )}
            </>
          )}
          <p className="mt-3 border-t border-border pt-2 font-mono text-xs text-muted-foreground">🔒 Classe oculta: ???</p>
        </section>

        <section className="border border-border bg-card p-4">
          <h2 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Resumo</h2>
          <ul className="mt-2 space-y-1.5 font-mono text-sm">
            <li>Missões hoje: <span className="text-foreground">{concluidas}/{missoesHoje.length}</span></li>
            <li>XP total acumulado: <span className="text-emerald-400">{fmt(personagem.xpTotal)}</span></li>
            <li>Dias perfeitos: <span className="text-amber-400">{personagem.diasPerfeitos}</span></li>
            <li>Chefes derrotados: <span className="text-red-400">{personagem.chefesDerrotados}</span></li>
            <li>Conquistas: <span className="text-cyan-400">{conquistas.filter(c => c.desbloqueada).length}/{conquistas.length}</span></li>
          </ul>
        </section>

        <section className="border border-border bg-card p-4">
          <h2 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">💰 Economias (registro manual)</h2>
          <p className="mt-2 font-mono text-2xl text-amber-300">£{fmt(personagem.economias)}</p>
          <form
            className="mt-3 flex gap-2"
            onSubmit={e => {
              e.preventDefault()
              const v = Number(economias)
              if (!Number.isNaN(v)) onEconomias(v)
            }}
          >
            <Input
              type="number"
              min="0"
              value={economias}
              onChange={e => setEconomias(e.target.value)}
              className="font-mono"
              aria-label="Total economizado"
            />
            <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Salvar</Button>
          </form>
          <p className="mt-2 text-xs text-muted-foreground">
            Alimenta a conquista “Primeiros £10.000”. Diagnóstico completo chega na Fase 3.
          </p>
        </section>
      </div>
    </div>
  )
}
