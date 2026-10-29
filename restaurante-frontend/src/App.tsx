import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { SessaoProvider, useSessao } from './sessao';
import { Home } from './paginas/Home';
import { Login } from './paginas/Login'; // <-- Importar a página de login

function Navbar() {
  const { sessao, logout } = useSessao();

  return (
    <header className="cabecalho">
      <div className="logo-container">
        <svg className="svg-logo" viewBox="0 0 24 24" width="24" height="24">
          <path fill="currentColor" d="M11 9H9V2H7v7H5V2H3v7c0 2.12 1.66 3.84 3.75 3.97V22h2.5v-9.03C11.34 12.84 13 11.12 13 9V2h-2v7zm5-3v8h2.5v8H21V2c-2.76 0-5 2.24-5 4z"/>
        </svg>
        <Link to="/" className="marca">RestauranteApi</Link>
      </div>
      <nav className="nav-links">
        <Link to="/">Início</Link>
        {sessao ? (
          <>
            <span className="usuario-saudacao">Olá, {sessao.nome}</span>
            <button onClick={logout} className="btn-sair">Sair</button>
          </>
        ) : (
          <Link to="/login" className="btn-login">Entrar</Link>
        )}
      </nav>
    </header>
  );
}

function Rodape() {
  return (
    <footer className="rodape">
      <p>Projeto escolar — empresa fictícia, não é um serviço real. Não use seu e-mail nem sua senha de verdade.</p>
    </footer>
  );
}

export function App() {
  return (
    <SessaoProvider>
      <BrowserRouter>
        <div className="app-layout">
          <a href="#conteudo-principal" className="pular-link">Pular para o conteúdo</a>
          <Navbar />
          <main id="conteudo-principal" className="conteudo-principal">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} /> {/* <-- Adicionar rota de login */}
              <Route path="*" element={<div className="container"><h2>Página não encontrada</h2><p>A rota solicitada não existe.</p></div>} />
            </Routes>
          </main>
          <Rodape />
        </div>
      </BrowserRouter>
    </SessaoProvider>
  );
}

export default App;