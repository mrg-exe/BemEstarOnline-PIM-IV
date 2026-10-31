using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class EnviarReceitaPage : ContentPage
{
    private readonly EnviarReceitaViewModel _vm;

    public EnviarReceitaPage(EnviarReceitaViewModel vm)
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
