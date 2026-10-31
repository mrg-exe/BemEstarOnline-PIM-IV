using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Login, cadastro e dados do usuário logado.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Resposta única do "esqueci minha senha": não revela se o e-mail tem conta.</summary>
    private const string RespostaEsqueci =
        "Se o e-mail estiver cadastrado, enviamos um link para criar uma nova senha. Ele vale por 30 minutos. Confira também a caixa de spam.";

    private readonly IAuthServico _auth;
    private readonly IRecuperacaoSenhaServico _recuperacao;

    public AuthController(IAuthServico auth, IRecuperacaoSenhaServico recuperacao)
    {
        _auth = auth;
        _recuperacao = recuperacao;
    }

    /// <summary>Esqueci minha senha (RF33): envia o link por e-mail. A resposta é sempre a mesma, exista ou não a conta.</summary>
    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha(EsqueciSenhaDto dto)
    {
        await _recuperacao.SolicitarAsync(dto.Email);
        return Ok(new { mensagem = RespostaEsqueci });
    }

    /// <summary>Cria a nova senha com o código do link do e-mail (uso único, 30 minutos).</summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(RecuperarSenhaDto dto)
    {
        await _recuperacao.RedefinirAsync(dto.Token, dto.NovaSenha);
        return Ok(new { mensagem = "Senha criada! Agora é só entrar com a senha nova." });
    }

    /// <summary>Entra com e-mail e senha e recebe o token de acesso.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginRespostaDto>> Login(LoginDto dto) => Ok(await _auth.LoginAsync(dto));

    /// <summary>Cria uma conta de cliente e já devolve o token (entra automaticamente).</summary>
    [HttpPost("registrar")]
    public async Task<ActionResult<LoginRespostaDto>> Registrar(RegistroDto dto) =>
        StatusCode(StatusCodes.Status201Created, await _auth.RegistrarAsync(dto));

    /// <summary>Dados de quem está logado (usado para conferir se o token ainda vale).</summary>
    [Authorize]
    [HttpGet("eu")]
    public async Task<ActionResult<UsuarioDto>> Eu() => Ok(await _auth.ObterAsync(User.ObterIdUsuario()));

    /// <summary>Troca a própria senha (pede a atual). Obrigatório no primeiro acesso de quem tem senha provisória.</summary>
    [Authorize]
    [HttpPost("alterar-senha")]
    public async Task<ActionResult<UsuarioDto>> AlterarSenha(AlterarSenhaDto dto) =>
        Ok(await _auth.AlterarSenhaAsync(User.ObterIdUsuario(), dto));

    /// <summary>Exclui a própria conta (pede a senha para confirmar). Os pedidos antigos ficam sem dados pessoais.</summary>
    [Authorize]
    [HttpPost("excluir-conta")]
    public async Task<IActionResult> ExcluirConta(ExcluirContaDto dto)
    {
        await _auth.ExcluirContaAsync(User.ObterIdUsuario(), dto.Senha);
        return NoContent();
    }
}
