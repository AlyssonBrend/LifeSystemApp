import { useCallback, useEffect, useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Toaster } from '@/components/ui/sonner'
import { toast } from 'sonner'
import { ApiErro, fmt, jogo, sessao } from '@/lib/api'
import type { AcaoResp, EstadoDto, EventoDto } from '@/lib/api'
import { AuthView } from '@/AuthView'
import { PainelTab } from '@/components/game/PainelTab'
import { MissoesTab } from '@/components/game/MissoesTab'
import { ChefeTab } from '@/components/game/ChefeTab'
import { LojaTab } from '@/components/game/LojaTab'
import { ConquistasTab } from '@/components/game/ConquistasTab'
import { FocoTab } from '@/components/game/FocoTab'
import { ClassesDialog, LevelUpOverlay, VitoriaDialog } from '@/components/game/Overlays'

export default function App() {
  const [logado, setLogado] = useState(() => !!sessao.token())
  if (!logado) return <RaizComToaster><AuthView onEntrar={() => setLogado(true)} /></RaizComToaster>
  return (
    <RaizComToaster>
      <GameView onSair={() => { sessao.sair(); setLogado(false) }} />
    </RaizComToaster>
  )
}

function RaizComToaster({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <Toaster position="top-center" theme="dark" richColors />
      {children}
    </div>
  )
}

function GameView({ onSair }: { onSair: () => void }) {
  const [estado, setEstado] = useState<EstadoDto | null>(null)
  const [erro, setErro] = useState('')
  const [levelUp, setLevelUp] = useState<EventoDto | null>(null)
  const [vitoria, setVitoria] = useState<EventoDto | null>(null)
  const [classesAberto, setClassesAberto] = useState(false)
  const [aba, setAba] = useState('missoes')

  const processarEventos = useCallback((eventos: EventoDto[], novo: EstadoDto) => {
    for (const ev of eventos) {
      switch (ev.tipo) {
        case 'levelup':
          setLevelUp(ev)
          if (ev.level === 5 && !novo.personagem.classe) setClassesAberto(true)
          break
        case 'vitoria':
          setVitoria(ev)
          break
        case 'conquista':
          toast.success(`${ev.emoji} Conquista: ${ev.nome}`, { description: '+100 🪙' })
          break
        case 'perfeito':
          toast.success('✨ DIA PERFEITO!', { description: '+100 XP · +20 🪙' })
          break
        case 'chefeCurou':
          toast.warning('👹 A sequência quebrou — o chefe recuperou 100 HP.')
          break
        case 'focoCompleto':
          toast.success('⏱️ Ciclo de foco completo!', { description: '+50 min de estudo · +10 XP · +1 🪙' })
          break
        case 'classe':
          toast.success('Classe escolhida! Missões das suas primárias agora pagam +20% XP.')
          break
        case 'compra':
          toast.info(`🎁 Recompensa comprada: ${ev.nome}. Aproveite sem culpa!`)
          break
      }
    }
  }, [])

  const despachar = useCallback(async (acao: () => Promise<AcaoResp>) => {
    try {
      const { estado: novo, eventos } = await acao()
      setEstado(novo)
      setErro('')
      processarEventos(eventos, novo)
    } catch (e) {
      if (e instanceof ApiErro && e.status === 401) onSair()
      else if (e instanceof ApiErro) toast.error(e.message)
      else setErro('Sem conexão com o servidor — suas ações precisam da API rodando.')
    }
  }, [onSair, processarEventos])

  useEffect(() => { despachar(jogo.estado) }, [despachar])

  // Enquanto houver foco ativo, ressincroniza a cada 30s (o servidor completa ciclos vencidos)
  useEffect(() => {
    if (!estado?.focoAtivo) return
    const t = setInterval(() => despachar(jogo.estado), 30_000)
    return () => clearInterval(t)
  }, [estado?.focoAtivo, despachar])

  if (!estado) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3">
        <p className="font-display uppercase tracking-[0.3em] text-amber-400">Life System</p>
        <p className="text-sm text-muted-foreground">{erro || 'Carregando seu personagem…'}</p>
        {erro && (
          <Button variant="secondary" onClick={() => despachar(jogo.estado)} className="font-display uppercase tracking-widest">
            Tentar de novo
          </Button>
        )}
      </div>
    )
  }

  const p = estado.personagem
  const pctXp = Math.min(100, (p.xpAtual / p.xpProximoLevel) * 100)

  return (
    <>
      {/* Painel do personagem (PRD 3.7) */}
      <header className="border-b border-border bg-card/60 px-4 py-4 backdrop-blur sm:px-8">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-8 gap-y-3">
          <div>
            <p className="font-display text-xs uppercase tracking-[0.3em] text-amber-400" data-testid="header-titulo">
              LEVEL {p.level} · {p.titulo} {p.emojiTitulo}
            </p>
            <h1 className="font-display text-2xl font-bold">{p.nome}</h1>
          </div>

          <div className="min-w-52 flex-1">
            <div className="flex justify-between font-mono text-xs text-muted-foreground">
              <span>EXP</span>
              <span data-testid="header-xp">{fmt(p.xpAtual)}/{fmt(p.xpProximoLevel)}</span>
            </div>
            <div className="mt-1 h-3 w-full border border-border bg-secondary" role="progressbar" aria-label="Experiência" aria-valuenow={p.xpAtual} aria-valuemax={p.xpProximoLevel}>
              <div className="h-full bg-gradient-to-r from-emerald-500 to-cyan-400 transition-all duration-500" style={{ width: `${pctXp}%` }} />
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 font-mono text-sm">
            <span title="Reflexo de Vitalidade/Recuperação">HP <span className="text-red-400">{p.hp}%</span></span>
            <span title="Qualidade do sono da última noite">⚡ <span className="text-cyan-400">{p.energia}%</span></span>
            <span data-testid="header-moedas">🪙 <span className="text-amber-400">{fmt(p.moedas)}</span></span>
            <span>💰 <span className="text-amber-300">£{fmt(p.economias)}</span></span>
            <span data-testid="header-streak">🔥 <span className="text-orange-400">{p.streakDias} {p.streakDias === 1 ? 'dia' : 'dias'}</span></span>
          </div>

          <Button size="sm" variant="ghost" className="ml-auto text-xs text-muted-foreground" onClick={onSair}>
            Sair
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-8">
        <Tabs value={aba} onValueChange={setAba}>
          <TabsList className="mb-5 grid h-auto w-full grid-cols-6 border border-border bg-card p-0 font-display">
            <TabsTrigger value="painel" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🧬 <span className="ml-1.5 hidden md:inline">Painel</span></TabsTrigger>
            <TabsTrigger value="missoes" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">⚔️ <span className="ml-1.5 hidden md:inline">Missões</span></TabsTrigger>
            <TabsTrigger value="foco" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">⏱️ <span className="ml-1.5 hidden md:inline">Foco</span></TabsTrigger>
            <TabsTrigger value="chefe" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">👹 <span className="ml-1.5 hidden md:inline">Chefe</span></TabsTrigger>
            <TabsTrigger value="loja" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🪙 <span className="ml-1.5 hidden md:inline">Loja</span></TabsTrigger>
            <TabsTrigger value="conquistas" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🏆 <span className="ml-1.5 hidden md:inline">Conquistas</span></TabsTrigger>
          </TabsList>

          <TabsContent value="painel">
            <PainelTab
              estado={estado}
              onEconomias={v => despachar(() => jogo.definirEconomias(v))}
              onAbrirClasses={() => setClassesAberto(true)}
            />
          </TabsContent>
          <TabsContent value="missoes">
            <MissoesTab
              estado={estado}
              onConcluir={id => despachar(() => jogo.concluirMissao(id))}
              onCheck={(id, i, marcado) => despachar(() => jogo.marcarCheck(id, i, marcado))}
              onAbrirFoco={() => setAba('foco')}
            />
          </TabsContent>
          <TabsContent value="foco">
            <FocoTab
              estado={estado}
              onIniciar={tipo => despachar(() => jogo.iniciarFoco(tipo))}
              onEncerrar={abandonar => despachar(() => jogo.encerrarFoco(abandonar))}
            />
          </TabsContent>
          <TabsContent value="chefe">
            <ChefeTab estado={estado} onRecompensa={t => despachar(() => jogo.definirRecompensa(t))} />
          </TabsContent>
          <TabsContent value="loja">
            <LojaTab
              estado={estado}
              onComprar={id => despachar(() => jogo.comprarItem(id))}
              onAdicionar={(n, pr) => despachar(() => jogo.adicionarItemLoja(n, pr))}
            />
          </TabsContent>
          <TabsContent value="conquistas">
            <ConquistasTab estado={estado} />
          </TabsContent>
        </Tabs>
      </main>

      <footer className="mx-auto max-w-6xl px-4 pb-6 sm:px-8">
        <p className="text-xs text-muted-foreground">
          Life System · MVP (Fase 1) · progresso salvo no servidor · regras do PRD v0.9
        </p>
      </footer>

      {levelUp && <LevelUpOverlay evento={levelUp} onFechar={() => setLevelUp(null)} />}
      {vitoria && <VitoriaDialog evento={vitoria} onFechar={() => setVitoria(null)} />}
      <ClassesDialog
        aberto={classesAberto && p.podeEscolherClasse}
        onFechar={() => setClassesAberto(false)}
        onEscolher={c => {
          despachar(() => jogo.escolherClasse(c))
          setClassesAberto(false)
        }}
      />
    </>
  )
}
