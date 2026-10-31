using System.Net;
using System.Text.Json;
using BemEstarOnline.Api.Dominio;

namespace BemEstarOnline.Api.Middlewares;

/// <summary>
/// Transforma os erros em respostas JSON padronizadas: { "mensagem": "..." }.
/// O site e o app mostram essa mensagem direto para o usuário.
/// Erros inesperados não revelam detalhes técnicos (segurança), mas ficam registrados no log.
/// </summary>
public class TratamentoDeErros
{
    private readonly RequestDelegate _proximo;
    private readonly ILogger<TratamentoDeErros> _log;

    public TratamentoDeErros(RequestDelegate proximo, ILogger<TratamentoDeErros> log)
    {
        _proximo = proximo;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _proximo(contexto);
        }
        catch (Exception ex)
        {
            var (status, mensagem) = ex switch
            {
                RegraDeNegocioException => (HttpStatusCode.BadRequest, ex.Message),
                NaoEncontradoException => (HttpStatusCode.NotFound, ex.Message),
                AcessoNegadoException => (HttpStatusCode.Forbidden, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "Tivemos um problema interno. Tente de novo em instantes.")
            };
            if (status == HttpStatusCode.InternalServerError) _log.LogError(ex, "Erro não tratado em {Caminho}", contexto.Request.Path);

            contexto.Response.StatusCode = (int)status;
            contexto.Response.ContentType = "application/json; charset=utf-8";
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(new { mensagem }));
        }
    }
}
