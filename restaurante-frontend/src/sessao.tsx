import React, { createContext, useContext, useState, useEffect } from 'react';

interface SessaoData {
  token: string;
  nome: string;
  papel: 'admin' | 'cliente';
  expiraEm: string;
}

interface SessaoContextType {
  sessao: SessaoData | null;
  login: (dados: SessaoData) => void;
  logout: () => void;
}

const SessaoContext = createContext<SessaoContextType | undefined>(undefined);

export function SessaoProvider({ children }: { children: React.ReactNode }) {
  const [sessao, setSessao] = useState<SessaoData | null>(() => {
    const salvo = localStorage.getItem('sessao');
    if (!salvo) return null;
    try {
      const dados: SessaoData = JSON.parse(salvo);
      if (new Date(dados.expiraEm) <= new Date()) {
        localStorage.removeItem('sessao');
        localStorage.removeItem('token');
        return null;
      }
      return dados;
    } catch {
      return null;
    }
  });

  const logout = () => {
    localStorage.removeItem('sessao');
    localStorage.removeItem('token');
    setSessao(null);
  };

  const login = (dados: SessaoData) => {
    localStorage.setItem('sessao', JSON.stringify(dados));
    localStorage.setItem('token', dados.token);
    setSessao(dados);
  };

  useEffect(() => {
    const interval = setInterval(() => {
      const salvo = localStorage.getItem('sessao');
      if (salvo) {
        try {
          const dados: SessaoData = JSON.parse(salvo);
          if (new Date(dados.expiraEm) <= new Date()) {
            logout();
            alert('Sua sessão expirou.');
          }
        } catch {
          logout();
        }
      }
    }, 10000);

    const handleSessaoExpirada = () => {
      logout();
      alert('Sua sessão expirou.');
    };

    window.addEventListener('sessao-expirada', handleSessaoExpirada as EventListener);
    return () => {
      clearInterval(interval);
      window.removeEventListener('sessao-expirada', handleSessaoExpirada as EventListener);
    };
  }, []);

  return (
    <SessaoContext.Provider value={{ sessao, login, logout }}>
      {children}
    </SessaoContext.Provider>
  );
}

export function useSessao() {
  const context = useContext(SessaoContext);
  if (!context) throw new Error('useSessao deve ser usado dentro de um SessaoProvider');
  return context;
}