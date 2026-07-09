import { useEffect, useMemo, useRef, useState } from 'react'
import { Button } from '@/components/ui/button'
import type { EstadoDto, FocoDto } from '@/lib/api'

interface Props {
  estado: EstadoDto
  onIniciar: (tipo: 'foco' | 'descanso') => void
  onEncerrar: (abandonar: boolean) => void
}

function formatar(segundos: number) {
  const s = Math.max(0, segundos)
  return `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(Math.floor(s % 60)).padStart(2, '0')}`
}

/** Cronômetro circular sincronizado com os timestamps do servidor (o frontend é só exibição, PRD 3.9). */
function Cronometro({ sessao, onEncerrar }: { sessao: FocoDto; onEncerrar: (abandonar: boolean) => void }) {
  // Prazo absoluto calculado uma única vez a partir do relógio do servidor
  const prazo = useMemo(
    () => Date.now() + (sessao.duracaoSegundos - sessao.decorridoSegundos) * 1000,
    [sessao.iniciadaEm, sessao.duracaoSegundos, sessao.decorridoSegundos],
  )
  const [restante, setRestante] = useState(() => Math.round((prazo - Date.now()) / 1000))
  const encerrouRef = useRef(false)

  useEffect(() => {
    const timer = setInterval(() => {
      const r = Math.round((prazo - Date.now()) / 1000)
      setRestante(r)
      if (r <= 0 && !encerrouRef.current) {
        encerrouRef.current = true
        onEncerrar(false) // ciclo completo — o servidor valida pelos timestamps
      }
    }, 500)
    return () => clearInterval(timer)
  }, [prazo, onEncerrar])

  const ehFoco = sessao.tipo === 'foco'
  const pct = Math.max(0, Math.min(1, restante / sessao.duracaoSegundos))
  const raio = 110
  const circunferencia = 2 * Math.PI * raio
  const cor = ehFoco ? 'stroke-amber-400' : 'stroke-cyan-400'

  return (
    <div className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-background/98 backdrop-blur" data-testid="foco-tela">
      <p className={`font-display text-sm uppercase tracking-[0.3em] ${ehFoco ? 'text-amber-400' : 'text-cyan-400'}`}>
        {ehFoco ? '⏱️ Modo Foco — 50 minutos' : '☕ Descanso — 10 minutos'}
      </p>

      <div className="relative mt-6">
        <svg width="260" height="260" viewBox="0 0 260 260" aria-hidden>
          <circle cx="130" cy="130" r={raio} fill="none" className="stroke-secondary" strokeWidth="10" />
          <circle
            cx="130" cy="130" r={raio} fill="none"
            className={`${cor} transition-all duration-500`}
            strokeWidth="10"
            strokeDasharray={circunferencia}
            strokeDashoffset={circunferencia * (1 - pct)}
            strokeLinecap="butt"
            transform="rotate(-90 130 130)"
          />
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="font-mono text-5xl font-bold" data-testid="foco-restante">{formatar(restante)}</span>
          <span className="mt-1 font-mono text-xs text-muted-foreground">{Math.round(pct * 100)}% restante</span>
        </div>
      </div>

      {ehFoco ? (
        <>
          <p className="mt-6 max-w-xs text-center text-sm text-muted-foreground">
            Navegação bloqueada. Saia da tela por mais de 30s e o ciclo não conta — sem punição, só honestidade.
          </p>
          <Button variant="ghost" className="mt-4 text-muted-foreground" onClick={() => onEncerrar(true)}>
            Desistir do ciclo
          </Button>
        </>
      ) : (
        <>
          <ul className="mt-6 space-y-1 text-center text-sm text-muted-foreground">
            <li>🧍 levante e alongue</li>
            <li>💧 beba água</li>
            <li>🌬️ respire fundo — longe de telas</li>
          </ul>
          <Button variant="ghost" className="mt-4 text-muted-foreground" onClick={() => onEncerrar(true)}>
            Encerrar descanso
          </Button>
        </>
      )}
    </div>
  )
}

export function FocoTab({ estado, onIniciar, onEncerrar }: Props) {
  const estudar = estado.missoesHoje.find(m => m.id === 'estudar')

  if (estado.focoAtivo) return <Cronometro sessao={estado.focoAtivo} onEncerrar={onEncerrar} />

  return (
    <div className="mx-auto max-w-lg space-y-4">
      <section className="border border-border bg-card p-5 text-center">
        <p className="font-display text-xs uppercase tracking-[0.3em] text-amber-400">Modo Foco 50/10</p>
        <p className="mt-2 text-sm text-muted-foreground">
          Ciclos de 50 minutos de foco + 10 de descanso. Cada ciclo completo alimenta a missão 📖 Estudar
          e paga +10 XP e +1 🪙. Início e fim são registrados <strong>no servidor</strong> — fechar o app
          não reinicia nem acelera o ciclo.
        </p>

        {estudar && (
          <div className="mt-4">
            <div className="flex items-center justify-between font-mono text-xs text-muted-foreground">
              <span>📖 Estudar hoje</span>
              <span>{estudar.progressoMinutos}/{estudar.minutosNecessarios} min {estudar.concluida && '· concluída ✓'}</span>
            </div>
            <div className="mt-1 h-2 w-full bg-secondary">
              <div
                className="h-full bg-gradient-to-r from-cyan-500 to-amber-400 transition-all"
                style={{ width: `${Math.min(100, (estudar.progressoMinutos / (estudar.minutosNecessarios ?? 120)) * 100)}%` }}
              />
            </div>
          </div>
        )}

        <div className="mt-5 grid gap-2 sm:grid-cols-2">
          <Button className="h-12 font-display text-base uppercase tracking-widest" onClick={() => onIniciar('foco')} data-testid="iniciar-foco">
            ⏱️ Focar 50min
          </Button>
          <Button variant="secondary" className="h-12 font-display text-base uppercase tracking-widest" onClick={() => onIniciar('descanso')}>
            ☕ Descansar 10min
          </Button>
        </div>
      </section>

      <p className="text-center text-xs text-muted-foreground">
        Técnica Pomodoro adaptada (PRD 3.9). No app nativo (Fase 5), o Não Perturbe do sistema será ativado junto.
      </p>
    </div>
  )
}
