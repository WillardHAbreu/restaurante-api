import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { api, expiracaoDoToken } from '../api'
import { useSessao } from '../sessao'

interface RespostaLogin {
  token: string
  email: string
  papel: 'admin' | 'cliente'
}

export function Login() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [carregando, setCarregando] = useState(false)

  const { login } = useSessao()
  const navigate = useNavigate()
  const location = useLocation()
  // a rota protegida manda { de: '/reservas' } no state; sem isso, volta para o início
  const destino = (location.state as { de?: string } | null)?.de ?? '/'

  useEffect(() => {
    document.title = 'Entrar · Restaurante Gourmet'
  }, [])

  async function entrar(e: FormEvent) {
    e.preventDefault()
    setErro(null)
    setCarregando(true)
    try {
      const resposta = await api<RespostaLogin>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, senha }),
      })
      login({
        token: resposta.token,
        nome: resposta.email.split('@')[0], // a API ainda não devolve o nome
        papel: resposta.papel,
        expiraEm: expiracaoDoToken(resposta.token),
      })
      navigate(destino, { replace: true })
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Falha ao entrar. Tente de novo.')
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="login-container">
      <h2>Entrar</h2>
      {erro && (
        <div className="alerta-erro" role="alert">
          {erro}
        </div>
      )}

      <form onSubmit={entrar}>
        <div className="grupo-form">
          <label htmlFor="email">E-mail</label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="grupo-form">
          <label htmlFor="senha">Senha</label>
          <input
            id="senha"
            type="password"
            autoComplete="current-password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
          />
        </div>

        <button type="submit" className="btn-primario btn-bloco" disabled={carregando}>
          {carregando ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </div>
  )
}
