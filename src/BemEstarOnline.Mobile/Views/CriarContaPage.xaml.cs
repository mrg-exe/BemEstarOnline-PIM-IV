using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class CriarContaPage : ContentPage
{
    public CriarContaPage(CriarContaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
