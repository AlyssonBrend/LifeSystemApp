import type { EstadoDto } from '@/lib/api'

export function ConquistasTab({ estado }: { estado: EstadoDto }) {
  const { conquistas } = estado
  const feitas = conquistas.filter(c => c.desbloqueada).length

  return (
    <div className="space-y-4">
      <div className="border border-border bg-card px-4 py-3">
        <p className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">
          Conquistas — <span className="text-foreground">{feitas}/{conquistas.length}</span> · cada uma vale +100 🪙
        </p>
      </div>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
        {conquistas.map(c => (
          <div
            key={c.id}
            data-testid={`conquista-${c.id}`}
            className={`border px-4 py-3 ${c.desbloqueada ? 'border-amber-500/60 bg-amber-500/5' : 'border-border bg-card opacity-60'}`}
          >
            <p className="font-display text-lg">
              {c.desbloqueada ? c.emoji : '🔒'} {c.nome} {c.desbloqueada && <span className="text-amber-400">✓</span>}
            </p>
            <p className="text-sm text-muted-foreground">{c.descricao}</p>
          </div>
        ))}
      </div>
      <div className="border border-border bg-card px-4 py-3 font-mono text-xs text-muted-foreground">
        🔒 Conquista oculta: ??? <span className="italic">(alguns segredos só se revelam a quem não tem pontos fracos…)</span>
      </div>
    </div>
  )
}
