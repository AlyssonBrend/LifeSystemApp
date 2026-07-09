import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import type { EventoDto } from '@/lib/api'
import { CLASSES, NOMES_ATRIBUTOS } from '@/lib/classes'

export function LevelUpOverlay({ evento, onFechar }: { evento: EventoDto; onFechar: () => void }) {
  return (
    <div
      className="fixed inset-0 z-50 flex cursor-pointer flex-col items-center justify-center bg-black/90"
      onClick={onFechar}
      role="dialog"
      aria-label="Level up"
      data-testid="levelup-overlay"
    >
      <p className="levelup-burst font-display text-6xl font-bold uppercase tracking-[0.2em] text-amber-400 sm:text-8xl">
        LEVEL UP
      </p>
      <p className="mt-4 font-mono text-2xl text-foreground">
        LEVEL {evento.level} · {evento.titulo}
      </p>
      <p className="mt-2 font-mono text-sm text-amber-300">+50 🪙</p>
      <p className="mt-8 text-xs uppercase tracking-widest text-muted-foreground">toque para continuar</p>
    </div>
  )
}

export function VitoriaDialog({ evento, onFechar }: { evento: EventoDto; onFechar: () => void }) {
  return (
    <Dialog open onOpenChange={aberto => !aberto && onFechar()}>
      <DialogContent className="border-amber-500/50" data-testid="vitoria-dialog">
        <DialogHeader>
          <DialogTitle className="font-display text-2xl uppercase tracking-widest text-amber-400">
            👑 Chefe derrotado!
          </DialogTitle>
          <DialogDescription className="sr-only">Recompensas da vitória sobre o chefe semanal</DialogDescription>
        </DialogHeader>
        <div className="space-y-3">
          <p className="text-lg">
            {evento.emoji} <span className="font-display uppercase">{evento.nome}</span> caiu diante da sua disciplina.
          </p>
          <ul className="space-y-1 font-mono text-sm">
            <li className="text-emerald-400">+1.000 XP</li>
            <li className="text-amber-400">+200 🪙</li>
            <li className="text-cyan-400">🎁 Caixa de recompensa: {evento.recompensa || 'defina a próxima na aba Chefe!'}</li>
          </ul>
          <p className="text-xs text-muted-foreground">O próximo chefe surge na segunda-feira, mais forte…</p>
          <Button className="w-full font-display uppercase tracking-widest" onClick={onFechar}>
            Coletar recompensas
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

interface ClassesProps {
  aberto: boolean
  onFechar: () => void
  onEscolher: (classe: string) => void
}

export function ClassesDialog({ aberto, onFechar, onEscolher }: ClassesProps) {
  return (
    <Dialog open={aberto} onOpenChange={a => !a && onFechar()}>
      <DialogContent className="max-h-[85vh] max-w-2xl overflow-y-auto" data-testid="classes-dialog">
        <DialogHeader>
          <DialogTitle className="font-display text-xl uppercase tracking-widest">O despertar — escolha sua classe</DialogTitle>
          <DialogDescription>
            Missões dos atributos primários pagam +20% XP — mas cada classe tem um piso de manutenção que não perdoa negligência.
          </DialogDescription>
        </DialogHeader>
        <div className="grid gap-2 sm:grid-cols-2">
          {CLASSES.map(c => (
            <button
              key={c.id}
              data-testid={`classe-${c.id}`}
              onClick={() => onEscolher(c.id)}
              className="border border-border bg-card px-4 py-3 text-left transition-colors hover:border-amber-500/60 hover:bg-amber-500/5"
            >
              <p className="font-display text-lg">{c.emoji} {c.nome}</p>
              <p className="mt-1 text-xs text-cyan-400">{c.primarias.map(p => NOMES_ATRIBUTOS[p]).join(' · ')}</p>
              <p className="mt-1 text-xs italic text-muted-foreground">“{c.lema}”</p>
              <p className="mt-1 font-mono text-[11px] text-muted-foreground">{c.progressao}</p>
            </button>
          ))}
        </div>
        <p className="font-mono text-xs text-muted-foreground">🔒 ??? — há quem diga que existe uma sétima… sem pontos fracos.</p>
      </DialogContent>
    </Dialog>
  )
}
