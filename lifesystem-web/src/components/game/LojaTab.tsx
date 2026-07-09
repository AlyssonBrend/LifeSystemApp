import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { fmt } from '@/lib/api'
import type { EstadoDto } from '@/lib/api'

interface Props {
  estado: EstadoDto
  onComprar: (itemId: number) => void
  onAdicionar: (nome: string, preco: number) => void
}

export function LojaTab({ estado, onComprar, onAdicionar }: Props) {
  const { personagem, loja, compras } = estado
  const [nome, setNome] = useState('')
  const [preco, setPreco] = useState('')

  return (
    <div className="grid gap-4 lg:grid-cols-[1.4fr_1fr]">
      <section>
        <div className="mb-3 flex items-center justify-between border border-border bg-card px-4 py-3">
          <h2 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Loja de recompensas</h2>
          <p className="font-mono text-lg text-amber-400">🪙 {fmt(personagem.moedas)}</p>
        </div>
        <div className="grid gap-2 sm:grid-cols-2">
          {loja.map(item => {
            const pode = personagem.moedas >= item.preco
            return (
              <div key={item.id} className="flex items-center justify-between gap-3 border border-border bg-card px-4 py-3" data-testid={`loja-item-${item.id}`}>
                <div>
                  <p className="text-sm">{item.nome}</p>
                  <p className="font-mono text-amber-400">{fmt(item.preco)} 🪙</p>
                </div>
                <Button
                  size="sm"
                  variant={pode ? 'default' : 'secondary'}
                  disabled={!pode}
                  onClick={() => onComprar(item.id)}
                  className="font-display uppercase tracking-widest"
                >
                  Comprar
                </Button>
              </div>
            )
          })}
        </div>
        <p className="mt-3 text-xs text-muted-foreground">
          Lazer comprado com disciplina, não com culpa. Na Fase 3, moedas poderão liberar seu orçamento real de recompensa (10 🪙 = £1).
        </p>
      </section>

      <div className="space-y-4">
        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Cadastrar recompensa</h3>
          <form
            className="mt-2 space-y-2"
            onSubmit={e => {
              e.preventDefault()
              const p = Number(preco)
              if (nome.trim() && p > 0) {
                onAdicionar(nome.trim(), Math.round(p))
                setNome('')
                setPreco('')
              }
            }}
          >
            <Input value={nome} onChange={e => setNome(e.target.value)} placeholder="ex.: tarde de cinema" aria-label="Nome da recompensa" />
            <div className="flex gap-2">
              <Input type="number" min="1" value={preco} onChange={e => setPreco(e.target.value)} placeholder="preço em 🪙" aria-label="Preço em moedas" className="font-mono" />
              <Button type="submit" variant="secondary" className="font-display uppercase tracking-widest">Adicionar</Button>
            </div>
          </form>
        </section>

        <section className="border border-border bg-card p-4">
          <h3 className="font-display text-sm uppercase tracking-[0.25em] text-muted-foreground">Extrato (últimas compras)</h3>
          {compras.length === 0 ? (
            <p className="mt-2 text-sm text-muted-foreground">Nenhuma compra ainda — complete missões para juntar moedas.</p>
          ) : (
            <ul className="mt-2 space-y-1.5">
              {compras.map((c, i) => (
                <li key={i} className="flex justify-between font-mono text-sm">
                  <span className="text-muted-foreground">
                    {new Date(c.em).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })} · {c.nome}
                  </span>
                  <span className="text-red-400">−{fmt(c.valor)} 🪙</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  )
}
