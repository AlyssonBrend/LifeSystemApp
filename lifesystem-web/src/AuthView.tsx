import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { auth, sessao, ApiErro } from '@/lib/api'

export function AuthView({ onEntrar }: { onEntrar: () => void }) {
  const [modo, setModo] = useState<'login' | 'registro'>('login')
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [nome, setNome] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  async function enviar(e: React.FormEvent) {
    e.preventDefault()
    setErro('')
    setCarregando(true)
    try {
      const resp = modo === 'login'
        ? await auth.login(email, senha)
        : await auth.registrar(email, senha, nome)
      sessao.entrar(resp.token)
      onEntrar()
    } catch (err) {
      setErro(err instanceof ApiErro ? err.message : 'Não consegui falar com o servidor')
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-sm border border-border bg-card p-6">
        <p className="font-display text-xs uppercase tracking-[0.3em] text-amber-400">Life System</p>
        <h1 className="mt-1 font-display text-2xl font-bold">
          {modo === 'login' ? 'Bem-vindo de volta' : 'O despertar começa aqui'}
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {modo === 'login'
            ? 'Entre para continuar sua evolução.'
            : 'Crie seu perfil — todo mundo parte do level 0.'}
        </p>

        <form onSubmit={enviar} className="mt-5 space-y-3">
          {modo === 'registro' && (
            <Input
              value={nome}
              onChange={e => setNome(e.target.value)}
              placeholder="Nome do personagem"
              aria-label="Nome do personagem"
              required
            />
          )}
          <Input
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            placeholder="E-mail"
            aria-label="E-mail"
            autoComplete="email"
            required
          />
          <Input
            type="password"
            value={senha}
            onChange={e => setSenha(e.target.value)}
            placeholder="Senha (mín. 6 caracteres)"
            aria-label="Senha"
            autoComplete={modo === 'login' ? 'current-password' : 'new-password'}
            minLength={6}
            required
          />
          {erro && <p className="text-sm text-red-400" role="alert">{erro}</p>}
          <Button type="submit" className="w-full font-display uppercase tracking-widest" disabled={carregando}>
            {carregando ? '...' : modo === 'login' ? 'Entrar' : 'Criar personagem'}
          </Button>
        </form>

        <button
          className="mt-4 w-full text-center text-sm text-muted-foreground underline-offset-4 hover:underline"
          onClick={() => { setModo(m => (m === 'login' ? 'registro' : 'login')); setErro('') }}
        >
          {modo === 'login' ? 'Não tem conta? Criar personagem' : 'Já tem conta? Entrar'}
        </button>
      </div>
    </div>
  )
}
