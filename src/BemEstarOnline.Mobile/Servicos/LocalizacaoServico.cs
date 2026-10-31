using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace BemEstarOnline.Mobile.Servicos;

public record CepEncontrado(string Logradouro, string Bairro, string Cidade, string Estado);

/// <summary>
/// Ajuda a cadastrar endereços (RF37), como o site faz: CEP pelo ViaCEP, posição pelo GPS do celular
/// e, sem GPS, as coordenadas pelo próprio endereço (OpenStreetMap/Nominatim). Se algo falhar, segue sem.
/// </summary>
public class LocalizacaoServico
{
    private readonly HttpClient _http;

    public LocalizacaoServico()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
        // O Nominatim pede que cada aplicativo se identifique.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("BemEstarOnline-PIM/1.0");
    }

    public async Task<CepEncontrado?> BuscarCepAsync(string cep)
    {
        var numeros = Validacao.SoNumeros(cep);
        if (numeros.Length != 8) return null;
        try
        {
            var d = await _http.GetFromJsonAsync<JsonElement>($"https://viacep.com.br/ws/{numeros}/json/");
            if (d.TryGetProperty("erro", out _)) return null;
            string Ler(string nome) => d.TryGetProperty(nome, out var v) ? v.GetString() ?? "" : "";
            return new CepEncontrado(Ler("logradouro"), Ler("bairro"), Ler("localidade"), Ler("uf"));
        }
        catch { return null; }
    }

    /// <summary>Posição atual pelo GPS (pede permissão na primeira vez).</summary>
    public async Task<(double Latitude, double Longitude)?> MinhaPosicaoAsync()
    {
        try
        {
            var permissao = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permissao != PermissionStatus.Granted) permissao = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permissao != PermissionStatus.Granted) return null;
            var local = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            return local is null ? null : (local.Latitude, local.Longitude);
        }
        catch { return null; }
    }

    /// <summary>Coordenadas a partir do endereço digitado (para o mapa da entrega).</summary>
    public async Task<(double Latitude, double Longitude)?> CoordenadasDoEnderecoAsync(string numero, string logradouro, string cidade, string estado)
    {
        try
        {
            var q = $"format=json&limit=1&country=Brasil&street={Uri.EscapeDataString($"{numero} {logradouro}")}" +
                    $"&city={Uri.EscapeDataString(cidade)}&state={Uri.EscapeDataString(estado)}";
            var lista = await _http.GetFromJsonAsync<JsonElement>($"https://nominatim.openstreetmap.org/search?{q}");
            if (lista.ValueKind != JsonValueKind.Array || lista.GetArrayLength() == 0) return null;
            var primeiro = lista[0];
            return (double.Parse(primeiro.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture),
                    double.Parse(primeiro.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture));
        }
        catch { return null; }
    }
}
