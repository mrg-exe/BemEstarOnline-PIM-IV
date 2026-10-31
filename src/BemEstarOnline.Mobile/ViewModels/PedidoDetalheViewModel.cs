using System.Collections.ObjectModel;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Rastreamento: linha do tempo (gravada pelos triggers do banco) e, quando o pedido sai
/// para entrega, o mapa com a posição do entregador atualizada a cada 8 segundos.
/// </summary>
public partial class PedidoDetalheViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ApiServico _api;
    private int _id;
    private IDispatcherTimer? _timer;
    private bool _mapaMontado;

    public ObservableCollection<Historico> Historico { get; } = new();
    public ObservableCollection<PedidoItem> Itens { get; } = new();

    /// <summary>A tela usa estes eventos para montar o mapa e mover o ponto do entregador.</summary>
    public event Action<string>? MapaMontar;
    public event Action<string>? MapaMover;

    [ObservableProperty] private string titulo = "";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string totalTexto = "";
    [ObservableProperty] private string endereco = "";
    [ObservableProperty] private bool podeCancelar;
    [ObservableProperty] private bool requerReceita;
    [ObservableProperty] private bool emEntrega;
    [ObservableProperty] private string textoEntrega = "";

    public PedidoDetalheViewModel(ApiServico api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && int.TryParse(id?.ToString(), out var numero))
        {
            _id = numero;
            _mapaMontado = false;
            _ = CarregarAsync();
        }
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Ocupado = true;
        try
        {
            var p = await _api.GetAsync<PedidoDetalhe>($"pedidos/{_id}");
            Titulo = $"Pedido nº {p.IdPedido}";
            Status = Texto.Status(p.Status);
            TotalTexto = $"Total: {Texto.Moeda(p.Total)} (produtos {Texto.Moeda(p.Subtotal)}, entrega {(p.Frete == 0 ? "grátis" : Texto.Moeda(p.Frete))}{(p.Desconto > 0 ? $", desconto {Texto.Moeda(p.Desconto)}" : "")})";
            Endereco = p.Endereco?.TextoCompleto ?? "";
            PodeCancelar = p.PodeCancelar;
            RequerReceita = p.RequerReceita && !p.ReceitaConferida;
            Historico.Clear();
            foreach (var h in p.Historico.OrderBy(h => h.AlteradoEm)) Historico.Add(h);
            Itens.Clear();
            foreach (var i in p.Itens) Itens.Add(i);

            EmEntrega = p.Status == "SAIU_PARA_ENTREGA";
            if (EmEntrega) { await AtualizarMapaAsync(); IniciarAcompanhamento(); }
            else PararAcompanhamento();
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        finally { Ocupado = false; }
    }

    private async Task AtualizarMapaAsync()
    {
        try
        {
            var r = await _api.GetAsync<Rastreio>($"pedidos/{_id}/rastreio");
            if (r.Status != "SAIU_PARA_ENTREGA")
            {
                PararAcompanhamento();
                await CarregarAsync(); // mudou de status (ex.: entregue)
                return;
            }

            var texto = $"{r.Entregador ?? "O entregador"} está a caminho.";
            if (r.EntregadorLatitude.HasValue && r.DestinoLatitude.HasValue)
            {
                var km = MapaHtml.DistanciaKm(r.EntregadorLatitude.Value, r.EntregadorLongitude!.Value, r.DestinoLatitude.Value, r.DestinoLongitude!.Value);
                texto += km < 1 ? $" Faltam cerca de {km * 1000:0} m." : $" Faltam cerca de {km:0.0} km.";
            }
            if (r.LocalizacaoEm.HasValue) texto += $" Atualizado às {r.LocalizacaoEm:HH:mm}.";
            TextoEntrega = texto;

            if (!_mapaMontado)
            {
                MapaMontar?.Invoke(MapaHtml.Endereco(r.LojaLatitude, r.LojaLongitude, r.DestinoLatitude, r.DestinoLongitude,
                                                     r.EntregadorLatitude, r.EntregadorLongitude));
                _mapaMontado = true;
            }
            else if (r.EntregadorLatitude.HasValue)
            {
                MapaMover?.Invoke(MapaHtml.Mover(r.EntregadorLatitude.Value, r.EntregadorLongitude!.Value));
            }
        }
        catch (ApiException ex) { TextoEntrega = ex.Message; }
    }

    private void IniciarAcompanhamento()
    {
        if (_timer is not null) return;
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(8);
        _timer.Tick += async (_, _) => await AtualizarMapaAsync();
        _timer.Start();
    }

    public void PararAcompanhamento()
    {
        _timer?.Stop();
        _timer = null;
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        if (!await ConfirmarAsync("Cancelar este pedido? Se já foi pago, o valor é estornado.")) return;
        try
        {
            await _api.PostAsync<PedidoDetalhe>($"pedidos/{_id}/cancelar");
            await AvisarAsync("Pedido cancelado.");
            await CarregarAsync();
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }
}
