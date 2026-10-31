using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class CatalogoPage : ContentPage
{
    private readonly CatalogoViewModel _vm;

    public CatalogoPage(CatalogoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Produtos.Count == 0) await _vm.CarregarCommand.ExecuteAsync(null);
    }
}
