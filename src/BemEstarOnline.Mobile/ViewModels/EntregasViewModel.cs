using System.Collections.ObjectModel;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Tela inicial do entregador: pedidos disponíveis e as entregas dele.
/// Consulta a API a cada 20 segundos e AVISA quando chega pedido novo,
/// dizendo se ele exige receita (o entregador precisa conferir na entrega).
/// </summary>
public partial class EntregasViewModel : BaseViewModel
{
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly HashSet<int> _jaAvisados = new();
    private IDispatcherTimer? _timer;
    private bool _primeiraCarga = true;

    public ObservableCollection<Entrega> MinhasEntregas { get; } = new();
    public ObservableCollection<Entrega> Disponiveis { get; } = new();

    [ObservableProperty] private string saudacao = "";
    [ObservableProperty] private bool atualizando;
    [ObservableProperty] private bool semDisponiveis;
    [ObservableProperty] private bool semMinhas;

    public EntregasViewModel(ApiServico api, SessaoServico sessao)
    {
        _api = api;
        _sessao = sessao;
    }

    public void Iniciar()
    {
        Saudacao = $"Olá, {_sessao.Usuario?.NomeExibicao.Split(' ')[0]}";
        _ = CarregarAsync();
        if (_timer is not null) return;
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(20);
        _timer.Tick += async (_, _) => await CarregarAsync();
        _timer.Start();
    }

    public void Parar()
    {
        _timer?.Stop();
        _timer = null;
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        try
        {
            var lista = await _api.GetAsync<List<Entrega>>("entregador/pedidos");
            MinhasEntregas.Clear();
            foreach (var e in lista.Where(e => e.MinhaEntrega)) MinhasEntregas.Add(e);
            Disponiveis.Clear();
            foreach (var e in lista.Where(e => !e.MinhaEntrega)) Disponiveis.Add(e);
            SemDisponiveis = Disponiveis.Count == 0;
            SemMinhas = MinhasEntregas.Count == 0;

            // Aviso de pedido novo (com a informação da receita)
            var novos = Disponiveis.Where(e => _jaAvisados.Add(e.IdPedido)).ToList();
            if (!_primeiraCarga && novos.Count > 0)
            {
                try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(600)); } catch { /* aparelho sem vibração */ }
                var linhas = string.Join("\n", novos.Select(n => $"Nº {n.IdPedido}: {(n.RequerReceita ? "EXIGE RECEITA" : "sem receita")}"));
                await AvisarAsync($"Novo pedido para entregar!\n{linhas}", "Pedido disponível");
            }
            _primeiraCarga = false;
        }
        catch (ApiException ex) when (ex.SemConexao) { /* tenta de novo no próximo ciclo */ }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        finally { Atualizando = false; }
    }

    [RelayCommand]
    private async Task AceitarAsync(Entrega entrega)
    {
        var aviso = entrega.RequerReceita
            ? "\n\nATENÇÃO: este pedido EXIGE RECEITA. Confira a receita do cliente antes de entregar."
            : "";
        if (!await ConfirmarAsync($"Pegar o pedido nº {entrega.IdPedido} para {entrega.EnderecoTexto}?{aviso}")) return;
        try
        {
            await _api.PostAsync<Entrega>($"entregador/pedidos/{entrega.IdPedido}/aceitar");
            await CarregarAsync();
            await AbrirAsync(entrega);
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); await CarregarAsync(); }
    }

    [RelayCommand]
    private Task AbrirAsync(Entrega entrega) => Shell.Current.GoToAsync($"entrega?id={entrega.IdPedido}");
}
