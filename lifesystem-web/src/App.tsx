import { useCallback, useEffect, useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Toaster } from '@/components/ui/sonner'
import { toast } from 'sonner'
import { ApiErro, fmt, jogo, sessao } from '@/lib/api'
import type { AcaoResp, CorpoDto, EstadoDto, EventoDto, FinancasDto, MenteDto, MentorDto } from '@/lib/api'
import { AuthView } from '@/AuthView'
import { CorpoTab } from '@/components/game/CorpoTab'
import { FinancasTab } from '@/components/game/FinancasTab'
import { MenteTab } from '@/components/game/MenteTab'
import { MentorTab } from '@/components/game/MentorTab'
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
  const [corpo, setCorpo] = useState<CorpoDto | null>(null)
  const [financas, setFinancas] = useState<FinancasDto | null>(null)
  const [mente, setMente] = useState<MenteDto | null>(null)
  const [mentor, setMentor] = useState<MentorDto | null>(null)
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
          toast.success('✨ DIA PERFEITO!', { description: '+100 XP · +20 🪙 · +1 🛡️ proteção de sequência' })
          break
        case 'chefeCurou':
          toast.warning('👹 A sequência quebrou — o chefe recuperou 100 HP.')
          break
        case 'streakProtegida':
          toast.info('🛡️ Sua proteção segurou a sequência — o streak sobreviveu!')
          break
        case 'transcendencia':
          toast.success('✨ VOCÊ DESPERTOU O AVATAR TRANSCENDENTE', { description: 'Sem pontos fracos. +10% de XP permanente em tudo.', duration: 10000 })
          break
        case 'transcendenciaPerdida':
          toast.warning('✨ A Transcendência se desfez — um dos atributos caiu abaixo de 80. Recupere-o.')
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
        case 'novoRecorde':
          toast.success(`💥 NOVO RECORDE — ${ev.nome}`, { description: ev.titulo, duration: 6000 })
          break
        case 'convite':
          toast.success(`🤝 Convite enviado para ${ev.nome}!`)
          break
        case 'metaPoupanca':
          toast.success('🐷 META DE POUPANÇA DO MÊS BATIDA!', { description: '+300 XP · +30 🪙 — missão mensal cumprida', duration: 6000 })
          break
        case 'dividaQuitada':
          toast.success(`⛓️ DÍVIDA QUITADA — ${ev.nome}`, { description: 'Um chefe a menos no bolso: +1.000 XP · +200 🪙', duration: 8000 })
          break
        case 'conversao':
          toast.success('💷 Moedas convertidas!', { description: ev.titulo })
          break
        case 'marco':
          toast.success(`🌳 Marco concluído — ${ev.nome}`, { description: `${ev.titulo} · +50 XP · +5 🪙` })
          break
        case 'livroConcluido':
          toast.success(`📕 Livro concluído: ${ev.nome}`, { description: '+100 XP · +10 🪙 — o Conhecimento agradece' })
          break
        case 'mentor':
          toast.success('🧙 O Mentor falou!', { description: 'Sua análise personalizada está pronta.' })
          break
      }
    }
  }, [])

  const despachar = useCallback(async (acao: () => Promise<AcaoResp>) => {
    try {
      const { estado: novo, eventos, corpo: novoCorpo, financas: novasFinancas, mente: novaMente, mentor: novoMentor } = await acao()
      setEstado(novo)
      if (novoCorpo) setCorpo(novoCorpo)
      if (novasFinancas) setFinancas(novasFinancas)
      if (novaMente) setMente(novaMente)
      if (novoMentor) setMentor(novoMentor)
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
      <header className={`border-b px-4 py-4 backdrop-blur sm:px-8 ${p.avatarTranscendente ? 'aura-transcendente border-amber-400/60' : 'border-border bg-card/60'}`}>
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
            {p.protecoesStreak > 0 && (
              <span title="Proteções de sequência (dia perfeito)">🛡️ <span className="text-cyan-400">×{p.protecoesStreak}</span></span>
            )}
          </div>

          <Button size="sm" variant="ghost" className="ml-auto text-xs text-muted-foreground" onClick={onSair}>
            Sair
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-8">
        <Tabs value={aba} onValueChange={setAba}>
          <TabsList className="mb-5 grid h-auto w-full grid-cols-10 border border-border bg-card p-0 font-display">
            <TabsTrigger value="painel" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🧬 <span className="ml-1.5 hidden lg:inline">Painel</span></TabsTrigger>
            <TabsTrigger value="missoes" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">⚔️ <span className="ml-1.5 hidden lg:inline">Missões</span></TabsTrigger>
            <TabsTrigger value="corpo" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🏋️ <span className="ml-1.5 hidden lg:inline">Corpo</span></TabsTrigger>
            <TabsTrigger value="mente" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🧠 <span className="ml-1.5 hidden lg:inline">Mente</span></TabsTrigger>
            <TabsTrigger value="financas" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">💰 <span className="ml-1.5 hidden lg:inline">Finanças</span></TabsTrigger>
            <TabsTrigger value="mentor" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🧙 <span className="ml-1.5 hidden lg:inline">Mentor</span></TabsTrigger>
            <TabsTrigger value="foco" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">⏱️ <span className="ml-1.5 hidden lg:inline">Foco</span></TabsTrigger>
            <TabsTrigger value="chefe" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">👹 <span className="ml-1.5 hidden lg:inline">Chefe</span></TabsTrigger>
            <TabsTrigger value="loja" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🪙 <span className="ml-1.5 hidden lg:inline">Loja</span></TabsTrigger>
            <TabsTrigger value="conquistas" className="rounded-none py-2.5 uppercase tracking-wider data-[state=active]:bg-secondary">🏆 <span className="ml-1.5 hidden lg:inline">Conquistas</span></TabsTrigger>
          </TabsList>

          <TabsContent value="painel">
            <PainelTab
              estado={estado}
              onIrFinancas={() => setAba('financas')}
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
          <TabsContent value="corpo">
            <CorpoTab
              corpo={corpo}
              onCarregar={() => despachar(jogo.corpo)}
              onPerfil={p => despachar(() => jogo.definirPerfil(p))}
              onAviso={() => despachar(jogo.aceitarAvisoSaude)}
              onCarga={(ex, kg, reps) => despachar(() => jogo.registrarCarga(ex, kg, reps))}
              onCardio={(km, min) => despachar(() => jogo.registrarCardio(km, min))}
              onOptIn={v => despachar(() => jogo.definirRankingOptIn(v))}
              onAmigo={c => despachar(() => jogo.adicionarAmigo(c))}
              onResponder={(id, aceitar) => despachar(() => jogo.responderAmizade(id, aceitar))}
            />
          </TabsContent>
          <TabsContent value="mente">
            <MenteTab
              mente={mente}
              onCarregar={() => despachar(jogo.mente)}
              onHabilidade={(trilhaId, nome) => despachar(() => jogo.criarHabilidade(trilhaId, nome))}
              onMarco={(id, i) => despachar(() => jogo.concluirMarco(id, i))}
              onLivro={(titulo, hid) => despachar(() => jogo.criarLivro(titulo, hid))}
              onConcluirLivro={id => despachar(() => jogo.concluirLivro(id))}
              onSocial={() => despachar(jogo.registrarSocial)}
              onAbrirFoco={() => setAba('foco')}
            />
          </TabsContent>
          <TabsContent value="financas">
            <FinancasTab
              financas={financas}
              moedas={p.moedas}
              economias={p.economias}
              onCarregar={() => despachar(jogo.financas)}
              onAviso={() => despachar(jogo.aceitarAvisoFinancas)}
              onPerfil={perfil => despachar(() => jogo.definirPerfilFinanceiro(perfil))}
              onAporte={v => despachar(() => jogo.registrarAporte(v))}
              onDivida={(nome, valor, juros) => despachar(() => jogo.criarDivida(nome, valor, juros))}
              onPagar={(id, valor) => despachar(() => jogo.pagarDivida(id, valor))}
              onConverter={m => despachar(() => jogo.converterMoedas(m))}
            />
          </TabsContent>
          <TabsContent value="mentor">
            <MentorTab
              mentor={mentor}
              onCarregar={() => despachar(jogo.mentor)}
              onAnalisar={() => despachar(jogo.analisarComMentor)}
            />
          </TabsContent>
          <TabsContent value="foco">
            <FocoTab
              estado={estado}
              mente={mente}
              onCarregarMente={() => despachar(jogo.mente)}
              onIniciar={(tipo, habilidadeId) => despachar(() => jogo.iniciarFoco(tipo, habilidadeId))}
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
          Life System · Fases 1–4 (Jogo + Corpo + Mente e Bolso + IA Mentora) · progresso salvo no servidor · regras do PRD v0.9
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
