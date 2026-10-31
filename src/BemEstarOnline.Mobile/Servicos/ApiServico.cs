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
