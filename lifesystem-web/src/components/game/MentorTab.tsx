import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import type { MentorDto } from '@/lib/api'

interface Props {
  mentor: MentorDto | null
  onCarregar: () => void
  onAnalisar: () => Promise<void> | void
}

function formatarData(iso: string) {
  const d = new Date(iso)
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) +
    ' ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

/** Renderização leve do markdown do Mentor (títulos ##, listas e negrito) — sem lib externa. */
function Conselho({ texto }: { texto: string }) {
  const linhas = texto.split('\n')
  return (
    <div className="space-y-1.5">
      {linhas.map((linha, i) => {
        const l = linha.trim()
        if (!l) return null
        if (l.startsWith('## '))
          return <h4 key={i} className="pt-2 font-display text-sm uppercase tracking-[0.2em] text-amber-400">{l.slice(3)}</h4>
        if (l.startsWith('- ') || l.startsWith('* '))
          return <p key={i} className="pl-3 text-sm text-muted-foreground">▸ {semNegrito(l.slice(2))}</p>
        if (/^\d+\.\s/.test(l))
          return <p key={i} className="pl-3 text-sm text-muted-foreground">▸ {semNegrito(l.replace(/^\d+\.\s/, ''))}</p>
        return <p key={i} className="text-sm text-muted-foreground">{semNegrito(l)}</p>
      })}
    </div>
  )
}

function semNegrito(s: string) {
  return s.replaceAll('**', '')
}

export function MentorTab({ mentor, onCarregar, onAnalisar }: Props) {
  const [analisando, setAnalisando] = useState(false)

  useEffect(() => { if (!mentor) onCarregar() }, [mentor, onCarregar])

  if (!mentor) return <p className="py-10 text-center text-sm text-muted-foreground">Invocando o Mentor…</p>

  if (!mentor.configurado) {
    return (
      <section className="mx-auto max-w-xl border border-border bg-card p-6 text-center">
        <p className="text-3xl">🧙</p>
        <h2 className="mt-2 font-display text-lg uppercase tracking-widest">O Mentor ainda dorme</h2>
        <p className="mt-3 text-sm text-muted-foreground">
          A IA Mentora usa a <strong>API Claude</strong> para analisar seu progresso real e personalizar os
          conselhos. Para despertá-la, configure a chave da API no servidor:
        </p>
        <pre className="mt-3 overflow-x-auto border border-border bg-secondary/40 px-3 py-2 text-left font-mono text-xs text-cyan-400">
          cd LifeSystem.Api{'\n'}dotnet user-secrets set Ia:Chave SUA-CHAVE-AQUI
        </pre>
        <p className="mt-3 text-xs text-muted-foreground">
          Depois reinicie a API. Sem a chave, todo o resto do jogo continua funcionando — os conselhos de
          Nível 1 (regras) seguem ativos nas abas Corpo, Mente e Finanças.
        </p>
      </section>
    )
  }

  return (
    <div className="mx-auto max-w-2xl space-y-4">
      <section className="border border-amber-400/30 bg-card p-5 text-center">
        <p className="font-display text-xs uppercase tracking-[0.3em] text-amber-400">🧙 IA Mentora</p>
        <p className="mt-2 text-sm text-muted-foreground">
          O Mentor lê seu progresso real — streak, atributos, chefe, finanças, árvore de conhecimento —
          e devolve uma análise personalizada com conselhos e um desafio para a semana.
        </p>
        <Button
          className="mt-4 h-12 w-full font-display text-base uppercase tracking-widest sm:w-auto sm:px-10"
          disabled={analisando || mentor.restantesHoje <= 0}
          onClick={async () => {
            setAnalisando(true)
            try { await onAnalisar() } finally { setAnalisando(false) }
          }}
          data-testid="analisar-mentor"
        >
          {analisando ? '🔮 Consultando o Mentor…' : '🔮 Analisar minha semana'}
        </Button>
        <p className="mt-2 font-mono text-xs text-muted-foreground">
          {mentor.restantesHoje}/{mentor.limiteDiario} análises restantes hoje
        </p>
      </section>

      {mentor.historico.length === 0 ? (
        <p className="border border-border bg-card p-4 text-center text-sm text-muted-foreground">
          Nenhuma análise ainda — invoque o Mentor acima.
        </p>
      ) : (
        mentor.historico.map(c => (
          <section key={c.id} className="border border-border bg-card p-4">
            <p className="font-mono text-xs text-muted-foreground">📜 {formatarData(c.criadoEm)}</p>
            <div className="mt-2">
              <Conselho texto={c.conteudo} />
            </div>
          </section>
        ))
      )}

      <p className="text-center text-xs text-muted-foreground">
        Conselhos educacionais gerados por IA — não substituem médico, nutricionista ou consultor financeiro.
      </p>
    </div>
  )
}
