using System.Security.Cryptography;

namespace BemEstarOnline.Api.Dominio.Entidades;

/// <summary>
/// Manifestação da ouvidoria (RF30): denúncia de discriminação, reclamação de atendimento ou
/// de entrega, sugestão ou elogio. Relato anônimo não guarda quem enviou nem o contato (LGPD).
/// </summary>
public class Ouvidoria
{
    // Sem 0/O e 1/I/L, para o protocolo não ser confundido ao ser lido ou digitado.
    private const string LetrasProtocolo = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public int IdOuvidoria { get; set; }
    public int? IdUsuario { get; set; }
    public string Tipo { get; set; } = TipoOuvidoria.Sugestao;
    public string? SubtipoDiscriminacao { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool Anonimo { get; set; }
    public string? Contato { get; set; }
    public string Protocolo { get; set; } = string.Empty;
    public string Status { get; set; } = StatusOuvidoria.Aberta;
    public string? Resposta { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? RespondidoEm { get; set; }

    public Usuario? Usuario { get; set; }

    public bool EhDiscriminacao => Tipo == TipoOuvidoria.Discriminacao;
    public bool EmAberto => StatusOuvidoria.EmAberto.Contains(Status);

    /// <summary>Cria a manifestação aplicando as regras (tipo, subtipo, texto e anonimato).</summary>
    public static Ouvidoria Registrar(string tipo, string? subtipo, string descricao, bool anonimo, int? idUsuario, string? contato)
    {
        tipo = (tipo ?? "").Trim().ToUpperInvariant();
        if (!TipoOuvidoria.Todos.Contains(tipo))
            throw new RegraDeNegocioException("Escolha o tipo de manifestação.");

        subtipo = string.IsNullOrWhiteSpace(subtipo) ? null : subtipo.Trim().ToUpperInvariant();
        if (tipo == TipoOuvidoria.Discriminacao)
        {
            if (subtipo is null || !Dominio.SubtipoDiscriminacao.Todos.Contains(subtipo))
                throw new RegraDeNegocioException("Escolha o tipo de discriminação.");
        }
        else subtipo = null;

        descricao = (descricao ?? "").Trim();
        if (descricao.Length < 10)
            throw new RegraDeNegocioException("Conte com um pouco mais de detalhes o que aconteceu (pelo menos 10 letras).");

        contato = string.IsNullOrWhiteSpace(contato) ? null : contato.Trim();
        return new Ouvidoria
        {
            Tipo = tipo,
            SubtipoDiscriminacao = subtipo,
            Descricao = descricao,
            Anonimo = anonimo,
            // Anônimo de verdade: nem a conta nem o contato ficam guardados.
            IdUsuario = anonimo ? null : idUsuario,
            Contato = anonimo ? null : contato,
            Protocolo = GerarProtocolo()
        };
    }

    /// <summary>
    /// Protocolo como "OUV-2026-7K3M9QXA". As 8 letras finais são sorteadas: um número em sequência
    /// (000123) deixaria qualquer pessoa adivinhar o protocolo de outra e ler a resposta.
    /// </summary>
    public static string GerarProtocolo()
    {
        var letras = new char[8];
        for (var i = 0; i < letras.Length; i++)
            letras[i] = LetrasProtocolo[RandomNumberGenerator.GetInt32(LetrasProtocolo.Length)];
        return $"OUV-{DateTime.Now:yyyy}-{new string(letras)}";
    }

    /// <summary>A equipe muda a situação e/ou responde. Responder já marca como "Respondida".</summary>
    public void Atualizar(string? status, string? resposta)
    {
        if (Status == StatusOuvidoria.Encerrada)
            throw new RegraDeNegocioException("Esta manifestação já foi encerrada.");

        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        resposta = string.IsNullOrWhiteSpace(resposta) ? null : resposta.Trim();
        if (status is null && resposta is null)
            throw new RegraDeNegocioException("Escreva a resposta ou escolha a nova situação.");
        if (status is not null && !StatusOuvidoria.Todos.Contains(status))
            throw new RegraDeNegocioException("Situação inválida.");

        if (resposta is not null)
        {
            Resposta = resposta;
            RespondidoEm = DateTime.Now;
            status ??= StatusOuvidoria.Respondida;
        }
        if (status == StatusOuvidoria.Respondida && Resposta is null)
            throw new RegraDeNegocioException("Escreva a resposta antes de marcar como respondida.");

        Status = status!;
    }
}
