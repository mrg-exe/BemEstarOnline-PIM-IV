using System.Security.Cryptography;
using BemEstarOnline.Api.Aplicacao.Interfaces;

namespace BemEstarOnline.Api.Infraestrutura.Seguranca;

/// <summary>
/// Guarda as senhas com PBKDF2 (SHA-256, 100 mil iterações e "sal" aleatório por usuário).
/// Assim, mesmo que alguém copie o banco, não consegue descobrir as senhas.
/// Formato salvo: pbkdf2$iteracoes$sal$hash (tudo em Base64).
/// </summary>
public class SenhaHasher : ISenhaHasher
{
    private const int Iteracoes = 100_000;
    private const int TamanhoSal = 16;
    private const int TamanhoHash = 32;

    public string Gerar(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"pbkdf2${Iteracoes}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string senha, string hashSalvo)
    {
        var partes = hashSalvo.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2" || !int.TryParse(partes[1], out var iteracoes)) return false;

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, sal, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
            // Comparação em tempo constante (evita ataques de tempo).
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
