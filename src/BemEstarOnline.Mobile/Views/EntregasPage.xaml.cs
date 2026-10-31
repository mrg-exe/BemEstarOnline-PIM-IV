using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class EntregasPage : ContentPage
{
    private readonly EntregasViewModel _vm;

    public EntregasPage(EntregasViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Iniciar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Parar();
    }
}
