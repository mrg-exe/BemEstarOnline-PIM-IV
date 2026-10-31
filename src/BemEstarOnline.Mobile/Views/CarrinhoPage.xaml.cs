using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class CarrinhoPage : ContentPage
{
    private readonly CarrinhoViewModel _vm;

    public CarrinhoPage(CarrinhoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
