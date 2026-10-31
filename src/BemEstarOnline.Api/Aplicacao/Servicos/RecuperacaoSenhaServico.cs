using System.Net;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>
/// Endereço público do site, lido do appsettings ("EnderecoDoSite"). Os links dos e-mails usam SEMPRE
/// este valor, nunca o endereço que veio na requisição: assim ninguém consegue forjar um link que
/// aponte para outro site e roube o código (ataque conhecido como "Host header injection").
/// </summary>
public record ConfiguracaoSite(string Endereco);

/// <summary>
/// "Esqueci minha senha" (RF33). A resposta é sempre a mesma, exista ou não a conta, para não revelar
/// quem é cliente. No máximo 3 pedidos por e-mail por hora; cada link vale 30 minutos e uma vez só,
/// e um pedido novo anula os anteriores.
/// </summary>
public class RecuperacaoSenhaServico : IRecuperacaoSenhaServico
{
    private const int MaximoPorHora = 3;

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IRedefinicaoSenhaRepositorio _pedidos;
    private readonly ISenhaHasher _hasher;
    private readonly IEmailServico _email;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly ConfiguracaoSite _site;
    private readonly ILogger<RecuperacaoSenhaServico> _log;

    public RecuperacaoSenhaServico(IUsuarioRepositorio usuarios, IRedefinicaoSenhaRepositorio pedidos, ISenhaHasher hasher,
                                   IEmailServico email, IUnidadeDeTrabalho uow, ConfiguracaoSite site,
                                   ILogger<RecuperacaoSenhaServico> log)
    {
        _usuarios = usuarios;
        _pedidos = pedidos;
        _hasher = hasher;
        _email = email;
        _uow = uow;
        _site = site;
        _log = log;
    }

    public async Task SolicitarAsync(string email)
    {
        var usuario = await _usuarios.ObterPorEmailAsync((email ?? "").Trim().ToLowerInvariant());
        if (usuario is null || !usuario.Ativo) return; // mesma resposta: não revela se a conta existe

        var recentes = await _pedidos.ListarDesdeAsync(usuario.IdUsuario, DateTime.Now.AddHours(-1));
        if (recentes.Count >= MaximoPorHora)
        {
            _log.LogWarning("Limite de {Max} pedidos de nova senha por hora atingido para a conta {Id}.", MaximoPorHora, usuario.IdUsuario);
            return;
        }
        foreach (var anterior in recentes) anterior.Anular(); // só o link mais novo vale

        var (pedido, token) = RedefinicaoSenha.Criar(usuario.IdUsuario);
        _pedidos.Adicionar(pedido);
        await _uow.SalvarAsync();

        var link = $"{_site.Endereco.TrimEnd('/')}/redefinir-senha.html?token={Uri.EscapeDataString(token)}";
        try
        {
            await _email.EnviarAsync(new MensagemEmail(usuario.Email, "Crie uma nova senha - Bem Estar Online",
                                                       ModeloEmail(usuario.NomeExibicao, link), link));
        }
        catch (Exception ex)
        {
            // Falha no envio não pode mudar a resposta (senão revelaria que a conta existe): só registra.
            _log.LogError(ex, "Não foi possível enviar o e-mail de nova senha da conta {Id}.", usuario.IdUsuario);
        }
    }

    public async Task RedefinirAsync(string token, string novaSenha)
    {
        var pedido = await _pedidos.ObterPorHashAsync(RedefinicaoSenha.CalcularHash(token));
        if (pedido is null || !pedido.Valida || pedido.Usuario is null || !pedido.Usuario.Ativo)
            throw new RegraDeNegocioException("Este link não vale mais (já foi usado ou passou de 30 minutos). Peça um novo em \"Esqueci minha senha\".");

        pedido.MarcarUsado();
        pedido.Usuario.DefinirNovaSenha(_hasher.Gerar(novaSenha));
        await _uow.SalvarAsync();
    }

    private static string ModeloEmail(string nome, string link)
    {
        var n = WebUtility.HtmlEncode(nome);
        var l = WebUtility.HtmlEncode(link);
        return $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:24px auto;background:#ffffff;border:1px solid #d3e1de;border-radius:12px;overflow:hidden">
              <div style="background:#00625f;color:#ffffff;padding:18px 24px;font-size:20px;font-weight:bold">Bem Estar Online</div>
              <div style="padding:24px;color:#0d3550;font-size:16px;line-height:1.5">
                <p style="margin-top:0">Olá, {n}!</p>
                <p>Recebemos um pedido para criar uma nova senha da sua conta. Clique no botão abaixo:</p>
                <p style="text-align:center;margin:28px 0">
                  <a href="{l}" style="background:#00807c;color:#ffffff;text-decoration:none;padding:14px 26px;border-radius:8px;font-weight:bold;display:inline-block">Criar nova senha</a>
                </p>
                <p>O link vale por <strong>30 minutos</strong> e funciona <strong>uma vez só</strong>.</p>
                <p>Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.</p>
                <p style="font-size:13px;color:#4a6378">Se o botão não abrir, copie este endereço no navegador:<br>{l}</p>
              </div>
              <div style="background:#f4f8f7;color:#4a6378;font-size:12px;padding:12px 24px">Farmácia Bem Estar Online, Cajamar/SP. Projeto acadêmico (PIM IV, UNIP).</div>
            </div>
            """;
    }
}
