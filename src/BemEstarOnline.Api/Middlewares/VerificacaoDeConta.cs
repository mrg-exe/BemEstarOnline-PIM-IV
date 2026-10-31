using System.Security.Claims;
using System.Text.Json;
using BemEstarOnline.Api.Dominio.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Seguranca;

namespace BemEstarOnline.Api.Middlewares;

/// <summary>
/// Confere a conta a cada requisição com token (RF35). O token vale 8 horas, mas a situação da conta
/// pode mudar antes disso:
///  - conta desativada (ou excluída) pelo administrador -> 401, a pessoa sai do sistema na hora;
///  - perfil mudado pelo administrador -> 401, para entrar de novo com o perfil novo;
///  - senha provisória ainda não trocada -> 403 em tudo, menos em "alterar senha" e "quem sou eu".
/// </summary>
public class VerificacaoDeConta
{
    private static readonly string[] LiberadosComSenhaProvisoria = { "/api/auth/alterar-senha", "/api/auth/eu" };
    private readonly RequestDelegate _proximo;

    public VerificacaoDeConta(RequestDelegate proximo) => _proximo = proximo;

    public async Task InvokeAsync(HttpContext contexto, IUsuarioRepositorio usuarios)
    {
        if (contexto.User.Identity?.IsAuthenticated == true)
        {
            var usuario = await usuarios.ObterPorIdAsync(contexto.User.ObterIdUsuario());
            if (usuario is null || !usuario.Ativo)
            {
                await ResponderAsync(contexto, StatusCodes.Status401Unauthorized, "Esta conta foi desativada. Fale com o administrador da farmácia.");
                return;
            }
            if (contexto.User.FindFirstValue(ClaimTypes.Role) != usuario.Perfil)
            {
                await ResponderAsync(contexto, StatusCodes.Status401Unauthorized, "Seu acesso foi alterado. Entre de novo.");
                return;
            }
            var caminho = contexto.Request.Path.Value?.TrimEnd('/').ToLowerInvariant() ?? "";
            if (usuario.TrocarSenha && caminho.StartsWith("/api/") && !LiberadosComSenhaProvisoria.Contains(caminho))
            {
                await ResponderAsync(contexto, StatusCodes.Status403Forbidden, "Sua senha é provisória. Crie uma nova senha para continuar.");
                return;
            }
        }
        await _proximo(contexto);
    }

    private static async Task ResponderAsync(HttpContext contexto, int status, string mensagem)
    {
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/json; charset=utf-8";
        await contexto.Response.WriteAsync(JsonSerializer.Serialize(new { mensagem }));
    }
}
