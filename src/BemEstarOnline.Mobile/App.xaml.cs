using BemEstarOnline.Mobile.Servicos;

namespace BemEstarOnline.Mobile;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell, AcessibilidadeServico acessibilidade, SincronizacaoServico sincronizacao)
    {
        InitializeComponent();
        // O app segue sempre o tema claro da marca; o modo escuro/alto contraste é escolhido no Perfil.
        UserAppTheme = AppTheme.Light;
        acessibilidade.Aplicar();
        _shell = shell;
        _ = sincronizacao.SincronizarAsync(); // envia pedidos que ficaram pendentes da última vez
    }

    // Abre a janela do app com o menu principal (AppShell).
    protected override Window CreateWindow(IActivationState? activationState) => new(_shell);
}
