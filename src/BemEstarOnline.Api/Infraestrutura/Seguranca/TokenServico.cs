using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio.Entidades;
using Microsoft.IdentityModel.Tokens;

namespace BemEstarOnline.Api.Infraestrutura.Seguranca;

/// <summary>Configurações do token, lidas da seção "Jwt" do appsettings.json.</summary>
public class JwtOpcoes
{
    public string Chave { get; set; } = string.Empty;
    public string Emissor { get; set; } = "BemEstarOnline.Api";
    public string Publico { get; set; } = "BemEstarOnline.Clientes";
    public int ExpiracaoHoras { get; set; } = 8;
}

/// <summary>
/// Gera o "crachá digital" (JWT) entregue no login. O site e o app enviam esse token
/// no cabeçalho Authorization: Bearer ... e a API sabe quem é a pessoa e qual o perfil dela.
/// </summary>
public class TokenServico : ITokenServico
{
    private readonly JwtOpcoes _opcoes;
    public TokenServico(JwtOpcoes opcoes) => _opcoes = opcoes;

    public (string token, DateTime expiraEm) Gerar(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddHours(_opcoes.ExpiracaoHoras);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.NomeExibicao),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Perfil)
        };

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Chave)), SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(_opcoes.Emissor, _opcoes.Publico, claims, DateTime.UtcNow, expira, credenciais);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}

public static class ClaimsExtensoes
{
    /// <summary>Lê o Id do usuário que veio dentro do token.</summary>
    public static int ObterIdUsuario(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? usuario.FindFirstValue("nameid")
                    ?? usuario.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(valor, out var id) ? id : 0;
    }

    public static bool EhEquipe(this ClaimsPrincipal usuario) =>
        usuario.IsInRole(Dominio.Perfis.Admin) || usuario.IsInRole(Dominio.Perfis.Atendente);
}
