using System.Security.Cryptography;
using System.Text;

namespace BemEstarOnline.Api.Dominio.Entidades;

/// <summary>
/// Pedido de "Esqueci minha senha" (RF33). O link enviado por e-mail leva um código aleatório;
/// no banco fica só o SHA-256 dele (como a senha, que fica só como hash). Vale 30 minutos e uma vez.
/// </summary>
public class RedefinicaoSenha
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(30);

    public int IdRedefinicao { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public DateTime? UsadoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;

    public Usuario? Usuario { get; set; }

    public bool Valida => UsadoEm is null && ExpiraEm > DateTime.Now;

    /// <summary>Cria o pedido e devolve também o código "puro", que vai só no e-mail (nunca para o banco).</summary>
    public static (RedefinicaoSenha pedido, string token) Criar(int idUsuario)
    {
        // 32 bytes aleatórios = impossível de adivinhar; em Base64 "de URL" para caber no link.
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                           .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var pedido = new RedefinicaoSenha
        {
            IdUsuario = idUsuario,
            TokenHash = CalcularHash(token),
            ExpiraEm = DateTime.Now.Add(Validade)
        };
        return (pedido, token);
    }

    public static string CalcularHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? ""))).ToLowerInvariant();

    public void MarcarUsado() => UsadoEm = DateTime.Now;

    /// <summary>Um pedido novo anula os anteriores: só o link mais recente funciona.</summary>
    public void Anular() { if (Valida) ExpiraEm = DateTime.Now; }
}
