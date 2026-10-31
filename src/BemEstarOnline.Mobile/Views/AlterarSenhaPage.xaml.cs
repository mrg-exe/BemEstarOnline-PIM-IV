using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class AlterarSenhaPage : ContentPage
{
    private readonly AlterarSenhaViewModel _vm;

    public AlterarSenhaPage(AlterarSenhaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Na troca obrigatória, o botão "voltar" do Android também não sai desta tela.
    protected override bool OnBackButtonPressed() => _vm.Obrigatoria || base.OnBackButtonPressed();
}
