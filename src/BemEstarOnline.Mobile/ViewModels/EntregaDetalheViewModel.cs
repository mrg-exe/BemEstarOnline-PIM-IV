using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Uma entrega: rota até o cliente, envio da localização (GPS real ou simulado para a apresentação),
/// aviso de receita e confirmação da entrega.
/// </summary>
public partial class EntregaDetalheViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ApiServico _api;
    private int _id;
    private IDispatcherTimer? _gps;
    private bool _gpsNegado; // a pessoa disse "não" à localização: não pergunta de novo
    private CancellationTokenSource? _simulacao;
    /// <summary>Endereço da farmácia (Centro de Cajamar), o mesmo da API: ponto de partida das entregas.</summary>
    private static readonly (double lat, double lng) Farmacia = (-23.35566, -46.87706);
    private (double lat, double lng) _posicao = Farmacia;

    public event Action<string>? MapaMontar;
    public event Action<string>? MapaMover;

    [ObservableProperty] private Entrega? entrega;
    [ObservableProperty] private bool podeSair;
    [ObservableProperty] private bool emRota;
    [ObservableProperty] private string situacaoGps = "";

    public EntregaDetalheViewModel(ApiServico api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && int.TryParse(id?.ToString(), out var n))
        {
            _id = n;
            _ = CarregarAsync();
        }
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        try
        {
            var lista = await _api.GetAsync<List<Entrega>>("entregador/pedidos");
            Entrega = lista.FirstOrDefault(e => e.IdPedido == _id);
            if (Entrega is null)
            {
                Parar();
                await AvisarAsync("Este pedido não está mais na sua lista (pode ter sido entregue ou cancelado).");
                await Shell.Current.GoToAsync("..");
                return;
            }
            PodeSair = Entrega.Status == "EM_SEPARACAO";
            EmRota = Entrega.Status == "SAIU_PARA_ENTREGA";
            MapaMontar?.Invoke(MapaHtml.Endereco(_posicao.lat, _posicao.lng, Entrega.DestinoLatitude, Entrega.DestinoLongitude,
                                                 EmRota ? _posicao.lat : null, EmRota ? _posicao.lng : null));
            if (EmRota) IniciarGps();
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }

    /// <summary>Abre o app de mapas do aparelho com a rota até o cliente.</summary>
    [RelayCommand]
    private async Task AbrirRotaAsync()
    {
        if (Entrega is null) return;
        try
        {
            var opcoes = new MapLaunchOptions { Name = Entrega.Cliente, NavigationMode = NavigationMode.Driving };
            if (Entrega.DestinoLatitude.HasValue)
                await Map.Default.OpenAsync(Entrega.DestinoLatitude.Value, Entrega.DestinoLongitude!.Value, opcoes);
            else
                await Map.Default.OpenAsync(new Placemark { Thoroughfare = Entrega.EnderecoTexto, CountryName = "Brasil" }, opcoes);
        }
        catch { await AvisarAsync("Não foi possível abrir o app de mapas neste aparelho."); }
    }

    [RelayCommand]
    private async Task LigarAsync()
    {
        if (string.IsNullOrWhiteSpace(Entrega?.Telefone)) return;
        try { PhoneDialer.Default.Open(new string(Entrega.Telefone.Where(char.IsDigit).ToArray())); }
        catch { await AvisarAsync($"Telefone do cliente: {Entrega.Telefone}"); }
    }

    [RelayCommand]
    private async Task SairParaEntregaAsync()
    {
        if (Entrega is null) return;
        if (Entrega.RequerReceita)
            await AvisarAsync("Lembrete: este pedido EXIGE RECEITA. Peça a receita ao cliente na entrega.", "Receita");
        try
        {
            await _api.PostAsync<Entrega>($"entregador/pedidos/{_id}/sair");
            _posicao = Farmacia; // saindo da farmácia
            await CarregarAsync();
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }

    /// <summary>Usa o GPS do aparelho a cada 10 segundos (se a pessoa permitir).</summary>
    private void IniciarGps()
    {
        if (_gps is not null || _simulacao is not null || _gpsNegado) return;
        _gps = Application.Current!.Dispatcher.CreateTimer();
        _gps.Interval = TimeSpan.FromSeconds(10);
        _gps.Tick += async (_, _) => await EnviarPosicaoRealAsync();
        _gps.Start();
        _ = EnviarPosicaoRealAsync();
    }

    private async Task EnviarPosicaoRealAsync()
    {
        try
        {
            var permissao = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permissao != PermissionStatus.Granted)
            {
                _gps?.Stop(); // pausa enquanto a pessoa responde, para não abrir outra pergunta
                permissao = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (permissao != PermissionStatus.Granted)
                {
                    // Não pergunta de novo: sem permissão, o caminho é o "Simular trajeto".
                    _gpsNegado = true;
                    _gps = null;
                    SituacaoGps = "Sem permissão de localização. Use \"Simular trajeto\".";
                    return;
                }
                _gps?.Start();
            }

            var local = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8)));
            if (local is null) { SituacaoGps = "GPS sem sinal. Use \"Simular trajeto\"."; return; }
            await EnviarAsync(local.Latitude, local.Longitude);
            SituacaoGps = $"GPS ativo. Posição enviada às {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception) { SituacaoGps = "GPS indisponível neste aparelho. Use \"Simular trajeto\"."; }
    }

    /// <summary>
    /// Para a apresentação (ou computador sem GPS): move o entregador da farmácia até o cliente
    /// em 20 passos, um a cada 2 segundos. O cliente vê o ponto andando no mapa dele.
    /// </summary>
    [RelayCommand]
    private async Task SimularTrajetoAsync()
    {
        if (Entrega is null || !EmRota) return;
        _gps?.Stop();
        _gps = null;
        _simulacao?.Cancel();
        _simulacao = new CancellationTokenSource();
        var token = _simulacao.Token;

        // Sempre sai da farmácia: se o GPS já tinha mandado a posição do computador, o trajeto
        // da apresentação começaria de lá (às vezes ao lado do cliente) e quase não apareceria.
        var inicio = Farmacia;
        var destino = (lat: Entrega.DestinoLatitude ?? inicio.lat - 0.02, lng: Entrega.DestinoLongitude ?? inicio.lng + 0.015);
        const int passos = 20;
        try
        {
            for (var i = 1; i <= passos && !token.IsCancellationRequested; i++)
            {
                var t = (double)i / passos;
                var curva = Math.Sin(t * Math.PI) * 0.003; // pequena curva para parecer rua
                await EnviarAsync(inicio.lat + (destino.lat - inicio.lat) * t + curva, inicio.lng + (destino.lng - inicio.lng) * t);
                SituacaoGps = $"Simulando trajeto: {i * 100 / passos}%";
                await Task.Delay(2000, token);
            }
            SituacaoGps = "Chegou ao endereço do cliente. Confirme a entrega.";
        }
        catch (TaskCanceledException) { }
        catch (ApiException ex) { SituacaoGps = ex.Message; }
        finally { _simulacao = null; }
    }

    private async Task EnviarAsync(double lat, double lng)
    {
        _posicao = (lat, lng);
        await _api.PutAsync<object>($"entregador/pedidos/{_id}/localizacao", new { latitude = lat, longitude = lng });
        MapaMover?.Invoke(MapaHtml.Mover(lat, lng));
    }

    [RelayCommand]
    private async Task ConfirmarEntregaAsync()
    {
        if (Entrega is null) return;
        var receitaConferida = false;
        if (Entrega.RequerReceita)
        {
            receitaConferida = await Shell.Current.DisplayAlertAsync("Receita obrigatória",
                "Este pedido tem remédio com tarja vermelha. Você conferiu a receita original do cliente (nome do paciente, data e assinatura do médico)?",
                "Sim, conferi", "Ainda não");
            if (!receitaConferida) return;
        }
        if (Entrega.CobrarNaEntrega &&
            !await ConfirmarAsync($"Recebeu {Texto.Moeda(Entrega.Total)} em dinheiro do cliente?")) return;

        try
        {
            await _api.PostAsync<Entrega>($"entregador/pedidos/{_id}/entregar", new { receitaConferida });
            Parar();
            await AvisarAsync("Entrega confirmada. Bom trabalho!");
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }

    public void Parar()
    {
        _gps?.Stop();
        _gps = null;
        _simulacao?.Cancel();
    }
}
