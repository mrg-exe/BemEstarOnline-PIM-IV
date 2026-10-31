using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class PedidoDetalhePage : ContentPage
{
    private readonly PedidoDetalheViewModel _vm;

    public PedidoDetalhePage(PedidoDetalheViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.MapaMontar += url => MainThread.BeginInvokeOnMainThread(() => Mapa.Source = new UrlWebViewSource { Url = url });
        _vm.MapaMover += js => MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await Mapa.EvaluateJavaScriptAsync(js); } catch { /* mapa ainda carregando */ }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.PararAcompanhamento(); // para de consultar a API quando a tela fecha
    }
}
