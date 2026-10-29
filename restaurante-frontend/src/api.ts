const URL_BASE = import.meta.env.VITE_API_URL ?? ''

export class ApiError extends Error {
  status: number

  constructor(status: number, mensagem: string) {
    super(mensagem)
    this.name = 'ApiError'
    this.status = status
  }
}

const MENSAGEM_PADRAO: Record<number, string> = {
  401: 'Você precisa entrar para fazer isso.',
  403: 'Você não tem permissão para fazer isso.',
  404: 'Não encontrado.',
}

// A API devolve dois formatos de erro:
//  - { mensagem } nas regras de negócio (400, 404, 409)
//  - ProblemDetails (detail / title / errors) na validação automática dos DTOs
async function lerMensagemDeErro(resposta: Response): Promise<string> {
  const padrao = MENSAGEM_PADRAO[resposta.status] ?? `Erro ${resposta.status}.`
  try {
    const corpo = await resposta.json()
    if (typeof corpo?.mensagem === 'string') return corpo.mensagem
    if (corpo?.errors && typeof corpo.errors === 'object') {
      const lista = Object.values(corpo.errors).flat().join(' ')
      if (lista) return lista
    }
    if (typeof corpo?.detail === 'string') return corpo.detail
    if (typeof corpo?.title === 'string') return corpo.title
  } catch {
    // corpo vazio (ex.: 403) ou que não é JSON: cai na mensagem padrão
  }
  return padrao
}

export async function api<T = unknown>(caminho: string, opcoes: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('token')

  const cabecalhos = new Headers(opcoes.headers)
  if (opcoes.body !== undefined) cabecalhos.set('Content-Type', 'application/json')
  if (token) cabecalhos.set('Authorization', `Bearer ${token}`)

  let resposta: Response
  try {
    resposta = await fetch(`${URL_BASE}/api${caminho}`, { ...opcoes, headers: cabecalhos })
  } catch {
    throw new Error(
      'Não foi possível falar com a API… Ela pode estar acordando (plano grátis); tente de novo em alguns segundos.',
    )
  }

  if (!resposta.ok) {
    // só derruba a sessão se havia token: um 401 sem token não é "sessão expirada"
    if (resposta.status === 401 && token) {
      window.dispatchEvent(new CustomEvent('sessao-expirada'))
    }
    throw new ApiError(resposta.status, await lerMensagemDeErro(resposta))
  }

  if (resposta.status === 204) return undefined as T
  return (await resposta.json()) as T
}

// O login devolve só { token, email, papel }; o expiraEm sai do campo "exp" do JWT.
export function expiracaoDoToken(token: string): string {
  try {
    const carga = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const { exp } = JSON.parse(atob(carga)) as { exp?: number }
    if (typeof exp === 'number') return new Date(exp * 1000).toISOString()
  } catch {
    // token fora do padrão: usa o prazo de 8 h que a API configura
  }
  return new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString()
}
