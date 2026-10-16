namespace Restaurante.Api.Resultado;

public class Resultado<T>
{
    public T? Valor { get; }
    public bool Sucesso { get; }
    public string MensagemErro { get; }
    public TipoErro? Tipo { get; }

    protected Resultado(T valor)
    {
        Sucesso = true;
        Valor = valor;
        MensagemErro = string.Empty;
        Tipo = null;
    }

    protected Resultado(string mensagemErro, TipoErro tipo)
    {
        Sucesso = false;
        Valor = default;
        MensagemErro = mensagemErro;
        Tipo = tipo;
    }

    public static Resultado<T> Ok(T valor) => new(valor);
    public static Resultado<T> Erro(string mensagem, TipoErro tipo) => new(mensagem, tipo);
}