import { useEffect, useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Switch } from '@/components/ui/switch'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { fmt, fmtPace } from '@/lib/api'
import type { CorpoDto, PerfilCorporalDto, RankingDto } from '@/lib/api'

interface Props {
  corpo: CorpoDto | null
  onCarregar: () => void
  onPerfil: (p: PerfilCorporalDto) => void
  onAviso: () => void
  onCarga: (exercicioId: string, cargaKg: number, reps: number) => void
  onCardio: (distanciaKm: number, duracaoMin: number) => void
  onOptIn: (v: boolean) => void
  onAmigo: (codigo: string) => void
  onResponder: (amizadeId: number, aceitar: boolean) => void
}

const rotulosAtividade: Record<string, string> = {
  sedentario: 'Sedentário (sem exercício)',
  leve: 'Leve (1–3× por semana)',
  moderado: 'Moderado (3–5× por semana)',
  intenso: 'Intenso (6–7× por semana)',
  atleta: 'Atleta (2× por dia)',
}

const rotulosObjetivo: Record<string, string> = {
  emagrecer: 'Emagrecer', manter: 'Manter', ganhar: 'Ganhar massa',
}

export function CorpoTab(props: Props) {
  const { corpo, onCarregar, onAviso } = props
  const [secao, setSecao] = useState<'treino' | 'cardio' | 'dieta' | 'rankings'>('treino')

  useEffect(() => { if (!corpo) onCarregar() }, [corpo, onCarregar])

  if (!corpo) return <p className="py-10 text-center text-sm text-muted-foreground">Carregando o módulo Corpo…</p>

  // Disclaimer obrigatório no primeiro uso (PRD 4.5)
  if (!corpo.avisoSaudeAceito) {
    return (
      <section className="mx-auto max-w-xl border border-amber-400/40 bg-card p-6 text-center">
        <p className="text-3xl">⚠️</p>
        <h2 className="mt-2 font-display text-lg uppercase tracking-widest">Antes de começar</h2>
        <p className="mt-3 text-sm text-muted-foreground">
          Os conselhos de dieta e treino do Life System são <strong>educacionais</strong>, calculados por fórmulas
          consagradas (Mifflin-St Jeor, Epley) — eles <strong>não substituem</strong> nutricionista, médico ou
          educador físico. Ajuste tudo à sua realidade e procure um profissional quando precisar.
        </p>
        <Button className="mt-5 font-display uppercase tracking-widest" onClick={onAviso}>
          Entendi, vamos treinar
        </Button>
      </section>
    )
  }

  return (
    <div className="space-y-4">
      {corpo.conselhos.length > 0 && (
        <section className="border border-cyan-400/30 bg-card p-4">
          <h3 className="font-display text-xs uppercase tracking-[0.25em] text-cyan-400">Conselheiro</h3>
          <ul className="mt-2 space-y-1">
            {corpo.conselhos.map((c, i) => (
              <li key={i} className="text-sm text-muted-foreground">▸ {c}</li>
            ))}
          </ul>
        </section>
      )}

      <div className="grid grid-cols-4 border border-border bg-card font-display text-xs sm:text-sm">
        {([['treino', '🏋️ Força'], ['cardio', '🏃 Cardio'], ['dieta', '🍗 Dieta'], ['rankings', '🏆 Rankings']] as const).map(([id, rotulo]) => (
          <button
            key={id}
            onClick={() => setSecao(id)}
            className={`py-2.5 uppercase tracking-wider transition-colors ${secao === id ? 'bg-secondary' : 'text-muted-foreground hover:text-foreground'}`}
          >
            {rotulo}
          </button>
        ))}
      </div>

      {secao === 'treino' && <SecaoTreino {...props} corpo={corpo} />}
      {secao === 'cardio' && <SecaoCardio {...props} corpo={corpo} />}
      {secao === 'dieta' && <SecaoDieta {...props} corpo={corpo} />}
      {secao === 'rankings' && <SecaoRankings {...props} corpo={corpo} />}
    </div>
  )
}

// ---------- Força (PRD 4.3) ----------

function SecaoTreino({ corpo, onCarga }: Props & { corpo: CorpoDto }) {
  const [exercicioId, setExercicioId] = useState('supino')
  const [carga, setCarga] = useState('')
  const [reps, setReps] = useState('')

  const historico = useMemo(
    () => corpo.cargas.filter(r => r.exercicioId === exercicioId).slice().reverse(),
    [corpo.cargas, exercicioId],
  )
  const exercicio = corpo.exercicios.find(e => e.id === exercicioId)

  return (
    <div className="grid gap-4 lg:grid-cols-[1.2fr_1fr]">
      <div className="space-y-4">
        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Registrar carga</h3>
          <form
            className="mt-3 flex flex-wrap items-end gap-2"
            onSubmit={e => {
              e.preventDefault()
              const c = Number(carga.replace(',', '.'))
              const r = Number(reps)
              if (c > 0 && r >= 1) { onCarga(exercicioId, c, Math.round(r)); setCarga(''); setReps('') }
            }}
          >
            <div className="min-w-40 flex-1">
              <Select value={exercicioId} onValueChange={setExercicioId}>
                <SelectTrigger aria-label="Exercício"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {corpo.exercicios.map(e => (
                    <SelectItem key={e.id} value={e.id}>{e.emoji} {e.nome}{e.basico ? ' ★' : ''}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <Input value={carga} onChange={e => setCarga(e.target.value)} placeholder="kg" aria-label="Carga em kg" inputMode="decimal" className="w-24 font-mono" />
            <Input value={reps} onChange={e => setReps(e.target.value)} placeholder="reps" aria-label="Repetições" inputMode="numeric" className="w-24 font-mono" />
            <Button type="submit" className="font-display uppercase tracking-widest">Registrar</Button>
          </form>
          <p className="mt-2 text-xs text-muted-foreground">
            ★ básicos: alimentam sua 💪 Força real (1RM ÷ peso corporal). Registrar treino completa a missão do dia.
          </p>
        </section>

        <section className="border border-border bg-card p-4">
          <div className="flex items-baseline justify-between">
            <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">
              Evolução — {exercicio?.nome}
            </h3>
            {exercicio?.melhorRm1 && (
              <p className="font-mono text-sm text-amber-400">1RM {fmt(exercicio.melhorRm1)} kg 💥</p>
            )}
          </div>
          {historico.length === 0 ? (
            <p className="mt-2 text-sm text-muted-foreground">Nenhum registro ainda — o primeiro já é um PR.</p>
          ) : (
            <>
              <MiniGrafico pontos={historico.map(r => r.rm1)} />
              <ul className="mt-2 max-h-48 space-y-1 overflow-y-auto">
                {historico.slice().reverse().slice(0, 12).map(r => (
                  <li key={r.id} className="flex justify-between font-mono text-sm">
                    <span className="text-muted-foreground">{formatarData(r.data)} · {fmt(r.cargaKg)} kg × {r.reps}</span>
                    <span>{r.rm1.toLocaleString('pt-BR')} kg {r.pr && <span className="text-amber-400">💥 PR</span>}</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </section>
      </div>

      <div className="space-y-4">
        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Seus recordes (1RM estimado)</h3>
          <ul className="mt-2 space-y-1.5">
            {corpo.exercicios.filter(e => e.melhorRm1).map(e => (
              <li key={e.id} className="flex justify-between text-sm">
                <span>{e.emoji} {e.nome}</span>
                <span className="font-mono text-amber-400">{fmt(e.melhorRm1!)} kg <span className="text-muted-foreground">({e.melhorMarca})</span></span>
              </li>
            ))}
            {corpo.exercicios.every(e => !e.melhorRm1) && (
              <li className="text-sm text-muted-foreground">Registre cargas para ver seus PRs aqui.</li>
            )}
          </ul>
        </section>

        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Fichas prontas</h3>
          <div className="mt-2 space-y-2">
            {corpo.fichas.map(f => (
              <details key={f.id} className="border border-border bg-secondary/40 px-3 py-2">
                <summary className="cursor-pointer text-sm">
                  <span className="font-display">{f.nome}</span>
                  <span className="ml-2 text-xs text-muted-foreground">{f.objetivo} · {f.frequencia}</span>
                </summary>
                {f.dias.map(d => (
                  <div key={d.nome} className="mt-2">
                    <p className="text-xs font-semibold text-cyan-400">{d.nome}</p>
                    <p className="text-xs text-muted-foreground">{d.exercicios.join(' · ')}</p>
                  </div>
                ))}
              </details>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}

// ---------- Cardio (PRD 4.3) ----------

function SecaoCardio({ corpo, onCardio }: Props & { corpo: CorpoDto }) {
  const [km, setKm] = useState('')
  const [min, setMin] = useState('')

  return (
    <div className="grid gap-4 lg:grid-cols-[1.2fr_1fr]">
      <div className="space-y-4">
        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Registrar corrida</h3>
          <form
            className="mt-3 flex flex-wrap items-end gap-2"
            onSubmit={e => {
              e.preventDefault()
              const k = Number(km.replace(',', '.'))
              const m = Number(min.replace(',', '.'))
              if (k > 0 && m > 0) { onCardio(k, m); setKm(''); setMin('') }
            }}
          >
            <Input value={km} onChange={e => setKm(e.target.value)} placeholder="distância (km)" aria-label="Distância em km" inputMode="decimal" className="w-40 font-mono" />
            <Input value={min} onChange={e => setMin(e.target.value)} placeholder="tempo (min)" aria-label="Tempo em minutos" inputMode="decimal" className="w-36 font-mono" />
            <Button type="submit" className="font-display uppercase tracking-widest">Registrar</Button>
          </form>
          <p className="mt-2 text-xs text-muted-foreground">
            O pace (min/km) é calculado no servidor. Corridas alimentam sua 🏃 Resistência e a missão de treino do dia.
          </p>
        </section>

        <section className="border border-border bg-card p-4">
          <div className="flex items-baseline justify-between">
            <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Histórico</h3>
            <p className="font-mono text-sm text-cyan-400">{corpo.kmMes.toLocaleString('pt-BR')} km em 30 dias</p>
          </div>
          {corpo.cardios.length === 0 ? (
            <p className="mt-2 text-sm text-muted-foreground">Nenhuma corrida registrada ainda.</p>
          ) : (
            <ul className="mt-2 max-h-64 space-y-1 overflow-y-auto">
              {corpo.cardios.map(r => (
                <li key={r.id} className="flex justify-between font-mono text-sm">
                  <span className="text-muted-foreground">
                    {formatarData(r.data)} · {r.distanciaKm.toLocaleString('pt-BR')} km em {r.duracaoMin.toLocaleString('pt-BR')} min
                  </span>
                  <span>{fmtPace(r.paceSegKm)}/km {r.pr && <span className="text-amber-400">💥 PR</span>}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section className="border border-border bg-card p-4">
        <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">PRs por faixa de distância</h3>
        <ul className="mt-2 space-y-1.5">
          {corpo.faixasCardio.map(f => (
            <li key={f.faixaKm} className="flex justify-between text-sm">
              <span>🏁 {f.faixaKm} km</span>
              {f.melhorPaceSegKm
                ? <span className="font-mono text-amber-400">{fmtPace(f.melhorPaceSegKm)}/km <span className="text-muted-foreground">({formatarData(f.melhorEm!)})</span></span>
                : <span className="font-mono text-muted-foreground">—</span>}
            </li>
          ))}
        </ul>
        <p className="mt-3 text-xs text-muted-foreground">
          A corrida entra na maior faixa que ela cobre (7 km → faixa de 5 km). PR premiado paga +50 XP +5 🪙 (1× por faixa por semana).
        </p>
      </section>
    </div>
  )
}

// ---------- Dieta (PRD 4.2) ----------

function SecaoDieta({ corpo, onPerfil }: Props & { corpo: CorpoDto }) {
  const p = corpo.perfil
  const [peso, setPeso] = useState(p ? String(p.pesoKg) : '')
  const [altura, setAltura] = useState(p ? String(p.alturaCm) : '')
  const [idade, setIdade] = useState(p ? String(p.idade) : '')
  const [sexo, setSexo] = useState<'m' | 'f'>(p?.sexo ?? 'm')
  const [atividade, setAtividade] = useState(p?.atividade ?? 'moderado')
  const [objetivo, setObjetivo] = useState(p?.objetivo ?? 'manter')
  const m = corpo.metas

  return (
    <div className="grid gap-4 lg:grid-cols-[1fr_1.2fr]">
      <section className="border border-border bg-card p-4">
        <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Perfil corporal</h3>
        <form
          className="mt-3 space-y-2"
          onSubmit={e => {
            e.preventDefault()
            const pk = Number(peso.replace(',', '.'))
            const ac = Math.round(Number(altura))
            const id = Math.round(Number(idade))
            if (pk > 0 && ac > 0 && id > 0)
              onPerfil({ pesoKg: pk, alturaCm: ac, idade: id, sexo, atividade, objetivo })
          }}
        >
          <div className="flex gap-2">
            <Input value={peso} onChange={e => setPeso(e.target.value)} placeholder="peso (kg)" aria-label="Peso em kg" inputMode="decimal" className="font-mono" />
            <Input value={altura} onChange={e => setAltura(e.target.value)} placeholder="altura (cm)" aria-label="Altura em cm" inputMode="numeric" className="font-mono" />
            <Input value={idade} onChange={e => setIdade(e.target.value)} placeholder="idade" aria-label="Idade" inputMode="numeric" className="font-mono" />
          </div>
          <Select value={sexo} onValueChange={v => setSexo(v as 'm' | 'f')}>
            <SelectTrigger aria-label="Sexo"><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="m">Masculino</SelectItem>
              <SelectItem value="f">Feminino</SelectItem>
            </SelectContent>
          </Select>
          <Select value={atividade} onValueChange={setAtividade}>
            <SelectTrigger aria-label="Nível de atividade"><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(rotulosAtividade).map(([v, r]) => <SelectItem key={v} value={v}>{r}</SelectItem>)}
            </SelectContent>
          </Select>
          <Select value={objetivo} onValueChange={setObjetivo}>
            <SelectTrigger aria-label="Objetivo"><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(rotulosObjetivo).map(([v, r]) => <SelectItem key={v} value={v}>{r}</SelectItem>)}
            </SelectContent>
          </Select>
          <Button type="submit" className="w-full font-display uppercase tracking-widest">
            {p ? 'Atualizar metas' : 'Calcular metas'}
          </Button>
        </form>
        <p className="mt-2 text-xs text-muted-foreground">
          Mifflin-St Jeor → calorias e macros. Com o perfil salvo, a missão 🍗 Alimentação passa a usar <strong>suas</strong> metas.
        </p>
      </section>

      <div className="space-y-4">
        {m ? (
          <section className="border border-border bg-card p-4">
            <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Suas metas diárias</h3>
            <div className="mt-3 grid grid-cols-3 gap-2 text-center sm:grid-cols-6">
              {([
                ['🔥', fmt(m.calorias), 'kcal'],
                ['🍗', `${m.proteinaG}g`, 'proteína'],
                ['🥑', `${m.gorduraG}g`, 'gordura'],
                ['🍚', `${m.carboG}g`, 'carbo'],
                ['🌾', `${m.fibrasG}g`, 'fibras'],
                ['💧', `${(m.aguaMl / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}L`, 'água'],
              ] as const).map(([emoji, valor, rotulo]) => (
                <div key={rotulo} className="border border-border bg-secondary/40 px-2 py-3">
                  <p className="text-lg">{emoji}</p>
                  <p className="font-mono text-sm text-amber-400">{valor}</p>
                  <p className="text-xs text-muted-foreground">{rotulo}</p>
                </div>
              ))}
            </div>
          </section>
        ) : (
          <section className="border border-border bg-card p-4">
            <p className="text-sm text-muted-foreground">Preencha o perfil ao lado para calcular calorias, macros e o plano de refeições.</p>
          </section>
        )}

        {corpo.plano.length > 0 && (
          <section className="border border-border bg-card p-4">
            <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Plano de refeições</h3>
            <ul className="mt-2 space-y-2">
              {corpo.plano.map(r => (
                <li key={r.nome} className="border-l-2 border-amber-400/50 pl-3">
                  <p className="text-sm">{r.nome} <span className="font-mono text-xs text-amber-400">~{fmt(r.kcal)} kcal</span></p>
                  <p className="text-xs text-muted-foreground">{r.sugestao}</p>
                </li>
              ))}
            </ul>
          </section>
        )}
      </div>
    </div>
  )
}

// ---------- Rankings e amigos (PRD 4.3) ----------

function SecaoRankings({ corpo, onOptIn, onAmigo, onResponder }: Props & { corpo: CorpoDto }) {
  const [codigo, setCodigo] = useState('')
  const [copiado, setCopiado] = useState(false)

  const copiar = () => {
    navigator.clipboard?.writeText(corpo.codigoAmigo).then(() => {
      setCopiado(true)
      setTimeout(() => setCopiado(false), 2000)
    })
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-4 lg:grid-cols-2">
        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Amigos</h3>
          <div className="mt-3 flex items-center justify-between border border-border bg-secondary/40 px-3 py-2">
            <div>
              <p className="text-xs text-muted-foreground">Seu código de convite</p>
              <p className="font-mono text-lg tracking-[0.3em] text-amber-400">{corpo.codigoAmigo}</p>
            </div>
            <Button size="sm" variant="secondary" onClick={copiar}>{copiado ? 'Copiado!' : 'Copiar'}</Button>
          </div>
          <form
            className="mt-2 flex gap-2"
            onSubmit={e => {
              e.preventDefault()
              if (codigo.trim()) { onAmigo(codigo.trim()); setCodigo('') }
            }}
          >
            <Input value={codigo} onChange={e => setCodigo(e.target.value.toUpperCase())} placeholder="código do amigo" aria-label="Código do amigo" className="font-mono uppercase" maxLength={6} />
            <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Convidar</Button>
          </form>
          <ul className="mt-3 space-y-1.5">
            {corpo.amigos.length === 0 && (
              <li className="text-sm text-muted-foreground">Sem amigos ainda — troque códigos e comparem PRs.</li>
            )}
            {corpo.amigos.map(a => (
              <li key={a.amizadeId} className="flex items-center justify-between text-sm">
                <span>🤝 {a.nome}</span>
                {a.situacao === 'aceita' && <span className="text-xs text-emerald-400">amigos</span>}
                {a.situacao === 'pendenteEnviado' && <span className="text-xs text-muted-foreground">convite enviado…</span>}
                {a.situacao === 'pendenteRecebido' && (
                  <span className="flex gap-1">
                    <Button size="sm" className="h-7 px-2 text-xs" onClick={() => onResponder(a.amizadeId, true)}>Aceitar</Button>
                    <Button size="sm" variant="secondary" className="h-7 px-2 text-xs" onClick={() => onResponder(a.amizadeId, false)}>Recusar</Button>
                  </span>
                )}
              </li>
            ))}
          </ul>
        </section>

        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Ranking geral</h3>
          <div className="mt-3 flex items-center justify-between">
            <div>
              <p className="text-sm">Participar do ranking geral</p>
              <p className="text-xs text-muted-foreground">Opt-in — seus PRs aparecem para todos, com selo "auto-relatado".</p>
            </div>
            <Switch checked={corpo.rankingOptIn} onCheckedChange={onOptIn} aria-label="Participar do ranking geral" />
          </div>
          <p className="mt-3 text-xs text-muted-foreground">
            A métrica padrão de força é <strong>relativa</strong> (1RM ÷ peso corporal) — senão a pessoa mais pesada
            sempre vence. O ranking entre amigos é o foco: amigos se conhecem, trapaça morre socialmente.
          </p>
        </section>
      </div>

      {corpo.rankingsForca.length === 0 && corpo.rankingsCardio.length === 0 ? (
        <p className="border border-border bg-card p-4 text-sm text-muted-foreground">
          Nenhum dado de ranking ainda — registre cargas e corridas.
        </p>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {[...corpo.rankingsForca, ...corpo.rankingsCardio].map(r => (
            <TabelaRanking key={r.chave} ranking={r} ehCardio={r.chave.startsWith('cardio')} />
          ))}
        </div>
      )}
    </div>
  )
}

function TabelaRanking({ ranking, ehCardio }: { ranking: RankingDto; ehCardio: boolean }) {
  const [escopo, setEscopo] = useState<'amigos' | 'geral'>('amigos')
  const entradas = escopo === 'amigos' ? ranking.amigos : ranking.geral

  return (
    <section className="border border-border bg-card p-4">
      <div className="flex items-center justify-between">
        <h3 className="font-display text-sm uppercase tracking-[0.2em]">{ranking.titulo}</h3>
        <div className="flex border border-border font-display text-xs">
          <button onClick={() => setEscopo('amigos')} className={`px-2 py-1 uppercase ${escopo === 'amigos' ? 'bg-secondary' : 'text-muted-foreground'}`}>Amigos</button>
          <button onClick={() => setEscopo('geral')} className={`px-2 py-1 uppercase ${escopo === 'geral' ? 'bg-secondary' : 'text-muted-foreground'}`}>Geral*</button>
        </div>
      </div>
      {escopo === 'geral' && <p className="mt-1 text-[10px] uppercase tracking-wider text-muted-foreground">* auto-relatado, sem verificação</p>}
      <ol className="mt-2 space-y-1">
        {entradas.length === 0 && <li className="text-sm text-muted-foreground">Ninguém aqui ainda.</li>}
        {entradas.map((e, i) => (
          <li key={i} className={`flex justify-between font-mono text-sm ${e.ehVoce ? 'text-amber-400' : ''}`}>
            <span>{i + 1}º {e.nome}{e.ehVoce ? ' (você)' : ''}</span>
            <span>
              {ehCardio
                ? `${fmtPace(e.valor)}/km`
                : e.relativo !== null
                  ? `${e.relativo.toLocaleString('pt-BR')}× peso · ${fmt(e.valor)} kg`
                  : `${fmt(e.valor)} kg`}
            </span>
          </li>
        ))}
      </ol>
    </section>
  )
}

// ---------- utilitários ----------

function MiniGrafico({ pontos }: { pontos: number[] }) {
  if (pontos.length < 2) return null
  const min = Math.min(...pontos)
  const max = Math.max(...pontos)
  const norm = (v: number) => (max === min ? 30 : 55 - ((v - min) / (max - min)) * 50)
  const passo = 300 / (pontos.length - 1)
  const d = pontos.map((v, i) => `${i === 0 ? 'M' : 'L'}${(i * passo).toFixed(1)},${norm(v).toFixed(1)}`).join(' ')
  return (
    <svg viewBox="0 0 300 60" className="mt-2 h-16 w-full" role="img" aria-label="Evolução do 1RM">
      <path d={d} fill="none" stroke="rgb(52 211 153)" strokeWidth="2" />
      {pontos.map((v, i) => (
        <circle key={i} cx={i * passo} cy={norm(v)} r="2.5" fill="rgb(251 191 36)" />
      ))}
    </svg>
  )
}

function formatarData(iso: string) {
  const [, m, d] = iso.split('-')
  return `${d}/${m}`
}
