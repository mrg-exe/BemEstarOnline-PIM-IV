namespace BemEstarOnline.Api.Dominio;

/// <summary>Erro de regra de negócio (vira resposta HTTP 400 com a mensagem para o usuário).</summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}

/// <summary>Registro não encontrado (vira resposta HTTP 404).</summary>
public class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string mensagem) : base(mensagem) { }
}

/// <summary>Usuário autenticado tentando acessar dado de outra pessoa (vira HTTP 403).</summary>
public class AcessoNegadoException : Exception
{
    public AcessoNegadoException(string mensagem = "Você não tem permissão para acessar este recurso.") : base(mensagem) { }
}
