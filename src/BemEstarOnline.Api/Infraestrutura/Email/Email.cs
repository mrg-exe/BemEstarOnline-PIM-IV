using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using BemEstarOnline.Api.Aplicacao.Interfaces;

namespace BemEstarOnline.Api.Infraestrutura.Email;

/// <summary>Seção "Smtp" do appsettings.json. Com Host vazio, a API usa o modo demonstração.</summary>
public class SmtpOpcoes
{
    public string Host { get; set; } = string.Empty;
    public int Porta { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Remetente { get; set; } = "nao-responda@bemestaronline.com.br";
    public string NomeRemetente { get; set; } = "Bem Estar Online";
    public bool UsarSsl { get; set; } = true;
}

/// <summary>Envia de verdade, por um servidor SMTP (ex.: Gmail com senha de app, Outlook, SendGrid).</summary>
public class EmailSmtp : IEmailServico
{
    private readonly SmtpOpcoes _opcoes;
    private readonly ILogger<EmailSmtp> _log;

    public EmailSmtp(SmtpOpcoes opcoes, ILogger<EmailSmtp> log)
    {
        _opcoes = opcoes;
        _log = log;
    }

    public async Task EnviarAsync(MensagemEmail mensagem)
    {
        using var email = new MailMessage
        {
            From = new MailAddress(_opcoes.Remetente, _opcoes.NomeRemetente),
            Subject = mensagem.Assunto,
            Body = mensagem.Html,
            IsBodyHtml = true
        };
        email.To.Add(mensagem.Para);
        using var cliente = new SmtpClient(_opcoes.Host, _opcoes.Porta)
        {
            EnableSsl = _opcoes.UsarSsl,
            Credentials = string.IsNullOrEmpty(_opcoes.Usuario) ? null : new NetworkCredential(_opcoes.Usuario, _opcoes.Senha)
        };
        await cliente.SendMailAsync(email);
        _log.LogInformation("E-mail \"{Assunto}\" enviado por SMTP.", mensagem.Assunto);
    }
}

/// <summary>
/// Modo demonstração (padrão): em vez de enviar, salva o e-mail como arquivo .html na pasta
/// "emails-enviados" da API e escreve o link no log. Assim o "Esqueci minha senha" funciona
/// na apresentação sem servidor de e-mail. A pasta não vai para o GitHub (.gitignore).
/// </summary>
public class EmailDemonstracao : IEmailServico
{
    private readonly string _pasta;
    private readonly ILogger<EmailDemonstracao> _log;

    public EmailDemonstracao(IWebHostEnvironment ambiente, ILogger<EmailDemonstracao> log)
    {
        _pasta = Path.Combine(ambiente.ContentRootPath, "emails-enviados");
        _log = log;
    }

    public async Task EnviarAsync(MensagemEmail mensagem)
    {
        Directory.CreateDirectory(_pasta);
        var paraArquivo = Regex.Replace(mensagem.Para, "[^a-zA-Z0-9.-]", "-");
        var arquivo = Path.Combine(_pasta, $"{DateTime.Now:yyyy-MM-dd_HHmmss}_{paraArquivo}.html");
        var pagina = $"""
            <!DOCTYPE html><html lang="pt-BR"><head><meta charset="UTF-8"><title>{WebUtility.HtmlEncode(mensagem.Assunto)}</title></head>
            <body style="margin:0;background:#eef3f2">
            <div style="font:14px/1.5 Arial,sans-serif;background:#fff8d6;border-bottom:1px solid #e6d27a;padding:10px 16px;color:#5a4a00">
              <strong>Modo demonstração:</strong> este e-mail não foi enviado de verdade, foi salvo pela API em emails-enviados.<br>
              <strong>Para:</strong> {WebUtility.HtmlEncode(mensagem.Para)} &nbsp;·&nbsp; <strong>Assunto:</strong> {WebUtility.HtmlEncode(mensagem.Assunto)}
              &nbsp;·&nbsp; <strong>Data:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}
            </div>
            {mensagem.Html}
            </body></html>
            """;
        await File.WriteAllTextAsync(arquivo, pagina);
        _log.LogInformation("E-mail de demonstração \"{Assunto}\" salvo em {Arquivo}. Link: {Link}",
            mensagem.Assunto, arquivo, mensagem.Link ?? "(sem link)");
    }
}
