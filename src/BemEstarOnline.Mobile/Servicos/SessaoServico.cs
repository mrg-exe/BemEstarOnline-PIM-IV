using System.Text.Json;
using BemEstarOnline.Mobile.Modelos;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>Endereço da API e dados do login (token JWT + usuário).</summary>
public class SessaoServico
{
    private readonly Armazenamento _armazenamento;
    public SessaoServico(Armazenamento armazenamento)
    {
        _armazenamento = armazenamento;
        AtualizarOrigem();
    }

    private void AtualizarOrigem()
    {
        var url = UrlApi.TrimEnd('/');
        Servidor.Origem = url.EndsWith("/api", StringComparison.OrdinalIgnoreCase) ? url[..^4] : url;
    }

    /// <summary>
    /// No emulador Android o "localhost" do computador se chama 10.0.2.2.
    /// No Windows é o próprio localhost. Pode ser trocado na tela de login.
    /// </summary>
    public static string UrlPadrao =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5088/api/" : "http://localhost:5088/api/";

    public string UrlApi
    {
        get => _armazenamento.Ler("url_api") ?? UrlPadrao;
        set
        {
            var url = value.Trim();
            if (!url.EndsWith('/')) url += "/";
            _armazenamento.Gravar("url_api", url);
            AtualizarOrigem();
        }
    }

    public string? Token
    {
        get
        {
            var expira = _armazenamento.Ler("expira_em");
            if (expira is not null && DateTime.TryParse(expira, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) && d < DateTime.UtcNow)
            {
                Sair();
                return null;
            }
            return _armazenamento.Ler("token");
        }
    }

    public Usuario? Usuario
    {
        get
        {
            var json = _armazenamento.Ler("usuario");
            return json is null ? null : JsonSerializer.Deserialize<Usuario>(json);
        }
    }

    public bool Logado => !string.IsNullOrEmpty(Token);
    public bool EhEntregador => Logado && Usuario?.EhEntregador == true;

    public void Entrar(LoginResposta resposta)
    {
        _armazenamento.Gravar("token", resposta.Token);
        _armazenamento.Gravar("expira_em", resposta.ExpiraEm.ToUniversalTime().ToString("o"));
        _armazenamento.Gravar("usuario", JsonSerializer.Serialize(resposta.Usuario));
    }

    /// <summary>Troca só os dados do usuário guardados (ex.: depois de alterar a senha), mantendo o token.</summary>
    public void AtualizarUsuario(Usuario usuario) =>
        _armazenamento.Gravar("usuario", JsonSerializer.Serialize(usuario));

    public void Sair()
    {
        _armazenamento.Gravar("token", null);
        _armazenamento.Gravar("expira_em", null);
        _armazenamento.Gravar("usuario", null);
    }
}
