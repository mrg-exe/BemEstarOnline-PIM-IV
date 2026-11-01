using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BemEstarOnline.Mobile.Modelos;

namespace BemEstarOnline.Mobile.Servicos;

public class ApiException : Exception
{
    public HttpStatusCode? Status { get; }
    public bool SemConexao => Status is null;
    public ApiException(string mensagem, HttpStatusCode? status) : base(mensagem) => Status = status;
}

/// <summary>Conversa com a mesma API usada pelo site (JSON + token JWT no cabeçalho).</summary>
public class ApiServico
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly SessaoServico _sessao;

    public ApiServico(SessaoServico sessao) => _sessao = sessao;

    public Task<T> GetAsync<T>(string caminho) => EnviarAsync<T>(HttpMethod.Get, caminho, null);
    public Task<T> PostAsync<T>(string caminho, object? corpo = null) => EnviarAsync<T>(HttpMethod.Post, caminho, corpo ?? new { });
    public Task<T> PutAsync<T>(string caminho, object corpo) => EnviarAsync<T>(HttpMethod.Put, caminho, corpo);
    public Task<T> PatchAsync<T>(string caminho, object corpo) => EnviarAsync<T>(HttpMethod.Patch, caminho, corpo);
    public Task DeleteAsync(string caminho) => EnviarAsync<object>(HttpMethod.Delete, caminho, null);
    /// <summary>Formulário com arquivo (multipart), ex.: a foto da receita (RF39).</summary>
    public Task<T> PostFormularioAsync<T>(string caminho, MultipartFormDataContent formulario) => EnviarAsync<T>(HttpMethod.Post, caminho, formulario);

    /// <summary>Confere rapidamente se a API responde.</summary>
    public async Task<bool> OnlineAsync()
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.None) return false;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var r = await _http.GetAsync(new Uri(new Uri(_sessao.UrlApi), "health"), cts.Token);
            return r.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>
    /// Botão "Testar conexão" da tela de entrada: diz em português o que está errado
    /// (endereço, rede, API desligada) em vez de só "sem conexão".
    /// </summary>
    public async Task<(bool Ok, string Mensagem)> DiagnosticarAsync(string urlApi)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.None)
            return (false, "O celular está sem internet/Wi-Fi. Ligue o Wi-Fi na mesma rede do computador.");
        if (!Uri.TryCreate(urlApi, UriKind.Absolute, out var baseUri))
            return (false, $"O endereço \"{urlApi}\" não é válido. Exemplo: http://192.168.18.7:5088/api/");
        var saude = new Uri(baseUri, "health");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var r = await _http.GetAsync(saude, cts.Token);
            return r.IsSuccessStatusCode
                ? (true, $"Tudo certo! A farmácia respondeu em {baseUri.Host}:{baseUri.Port}. Agora é só entrar.")
                : (false, $"O computador respondeu, mas com erro {(int)r.StatusCode}. Confira se o endereço termina em /api/.");
        }
        catch (OperationCanceledException)
        {
            return (false, $"Ninguém respondeu em {baseUri.Host}:{baseUri.Port} (6 segundos). Confira: o celular está no MESMO Wi-Fi do " +
                           "computador? O IP está certo (ipconfig no computador)? O Docker está ligado? Teste também " +
                           $"http://{baseUri.Host}:{baseUri.Port} no navegador do celular.");
        }
        catch (Exception ex)
        {
            var detalhe = ex.InnerException?.Message ?? ex.Message;
            if (baseUri.Host is "localhost" or "127.0.0.1" && DeviceInfo.Platform == DevicePlatform.Android)
                return (false, "No celular, \"localhost\" é o próprio celular. Use o IP do computador, ex.: http://192.168.18.7:5088/api/");
            if (detalhe.Contains("refused", StringComparison.OrdinalIgnoreCase))
                return (false, $"O computador {baseUri.Host} foi encontrado, mas a API não está ligada na porta {baseUri.Port}. " +
                               "Abra o Docker Desktop e rode \"docker compose up -d\".");
            return (false, $"Não deu para falar com {baseUri.Host}:{baseUri.Port}. Detalhe técnico: {detalhe}");
        }
    }

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string caminho, object? corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(new Uri(_sessao.UrlApi), caminho.TrimStart('/')));
        var token = _sessao.Token;
        if (!string.IsNullOrEmpty(token)) requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is HttpContent conteudo) requisicao.Content = conteudo;
        else if (corpo is not null) requisicao.Content = JsonContent.Create(corpo, options: Json);

        HttpResponseMessage resposta;
        try
        {
            resposta = await _http.SendAsync(requisicao);
        }
        catch (Exception)
        {
            throw new ApiException("Sem conexão com o servidor da farmácia.", null);
        }

        if (resposta.IsSuccessStatusCode)
        {
            if (resposta.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object)) return default!;
            return (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
        }

        if (resposta.StatusCode == HttpStatusCode.Unauthorized && token is not null)
        {
            _sessao.Sair();
            throw new ApiException("Sua sessão expirou. Entre de novo.", resposta.StatusCode);
        }

        string mensagem;
        try { mensagem = (await resposta.Content.ReadFromJsonAsync<ErroApi>(Json))?.Mensagem ?? "Não foi possível concluir."; }
        catch { mensagem = "Não foi possível concluir."; }
        throw new ApiException(mensagem, resposta.StatusCode);
    }
}
