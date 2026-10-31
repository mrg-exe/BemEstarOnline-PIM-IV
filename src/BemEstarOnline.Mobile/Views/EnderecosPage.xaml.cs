using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class EnderecosPage : ContentPage
{
    private readonly EnderecosViewModel _vm;

    public EnderecosPage(EnderecosViewModel vm)
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
