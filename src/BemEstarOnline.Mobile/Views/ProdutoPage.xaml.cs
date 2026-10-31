using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class ProdutoPage : ContentPage
{
    private readonly ProdutoViewModel _vm;

    public ProdutoPage(ProdutoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

}
