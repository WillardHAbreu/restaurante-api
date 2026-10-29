import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../api'

interface Mesa {
  id: number
  numeroOuNome: string
  capacidadePessoas: number
  localizacao: string
}

interface PaginaDeMesas {
  total: number
  pagina: number
  tamanho: number
  totalPaginas: number
  itens: Mesa[]
}

interface Estado {
  chave: string // "busca|pagina" da requisição a que estes dados respondem
  dados: PaginaDeMesas | null
  erro: string | null
}

const POR_PAGINA = 6

export function Home() {
  const [params, setParams] = useSearchParams()
  const busca = params.get('busca') ?? ''
  const pagina = Math.max(1, Number(params.get('pagina')) || 1)

  const [termo, setTermo] = useState(busca)
  const [estado, setEstado] = useState<Estado>({ chave: '', dados: null, erro: null })

  const chave = `${busca}|${pagina}`
  // enquanto a resposta guardada não for da requisição atual, está carregando
  const carregando = estado.chave !== chave

  useEffect(() => {
    document.title = 'Início · Restaurante Gourmet'
  }, [])

  useEffect(() => {
    let ativo = true
    const consulta = new URLSearchParams({ pagina: String(pagina), tamanho: String(POR_PAGINA) })
    if (busca) consulta.set('busca', busca)

    api<PaginaDeMesas>(`/mesas?${consulta}`)
      .then((dados) => {
        if (ativo) setEstado({ chave, dados, erro: null })
      })
      .catch((err) => {
        if (ativo) {
          setEstado({
            chave,
            dados: null,
            erro: err instanceof Error ? err.message : 'Erro inesperado.',
          })
        }
      })

    return () => {
      ativo = false
    }
  }, [chave, busca, pagina])

  function irPara(novaBusca: string, novaPagina: number) {
    const proximos = new URLSearchParams()
    if (novaBusca) proximos.set('busca', novaBusca)
    if (novaPagina > 1) proximos.set('pagina', String(novaPagina))
    setParams(proximos)
  }

  function buscar(e: FormEvent) {
    e.preventDefault()
    irPara(termo.trim(), 1)
  }

  function limpar() {
    setTermo('')
    irPara('', 1)
  }

  const dados = estado.dados

  return (
    <div className="container">
      <section className="hero">
        <h1>Sabores inesquecíveis, mesa garantida</h1>
        <p>Escolha uma mesa e reserve online em poucos cliques.</p>
      </section>

      <form onSubmit={buscar} className="busca-form" role="search">
        <input
          type="search"
          aria-label="Buscar mesa"
          placeholder="Buscar por nome ou localização…"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
        />
        <button type="submit" className="btn-primario">
          Buscar
        </button>
        {busca && (
          <button type="button" onClick={limpar} className="btn-secundario">
            Limpar
          </button>
        )}
      </form>

      {!carregando && estado.erro && (
        <div className="alerta-erro" role="alert">
          {estado.erro}
        </div>
      )}

      {carregando ? (
        <div className="grid-cards" aria-busy="true" aria-label="Carregando mesas">
          {Array.from({ length: POR_PAGINA }, (_, n) => (
            <div key={n} className="card esqueleto">
              <div className="esqueleto-linha curta" />
              <div className="esqueleto-linha" />
              <div className="esqueleto-linha media" />
            </div>
          ))}
        </div>
      ) : dados && dados.itens.length === 0 ? (
        <div className="estado-vazio">
          {busca ? (
            <>
              <p>Nenhuma mesa encontrada para “{busca}”.</p>
              <button onClick={limpar} className="btn-primario">
                Limpar busca e ver todas
              </button>
            </>
          ) : (
            <p>Ainda não há mesas cadastradas.</p>
          )}
        </div>
      ) : dados ? (
        <>
          <div className="grid-cards">
            {dados.itens.map((mesa) => (
              <article key={mesa.id} className="card">
                <div className="card-header">
                  <h3>{mesa.numeroOuNome}</h3>
                  <span className="etiqueta">{mesa.localizacao || 'Salão'}</span>
                </div>
                <p className="destaque">
                  Até <strong>{mesa.capacidadePessoas}</strong> pessoas
                </p>
                <Link to={`/mesas/${mesa.id}`} className="link-detalhe">
                  Ver detalhes →
                </Link>
              </article>
            ))}
          </div>

          {dados.totalPaginas > 1 && (
            <nav className="paginacao" aria-label="Paginação">
              <button
                className="btn-secundario"
                disabled={pagina <= 1}
                onClick={() => irPara(busca, pagina - 1)}
              >
                ← Anterior
              </button>
              <span>
                Página {dados.pagina} de {dados.totalPaginas}
              </span>
              <button
                className="btn-secundario"
                disabled={pagina >= dados.totalPaginas}
                onClick={() => irPara(busca, pagina + 1)}
              >
                Próxima →
              </button>
            </nav>
          )}
        </>
      ) : null}
    </div>
  )
}
