namespace BemEstarOnline.Mobile.WinUI;

/// <summary>Ponto de entrada quando o app roda no Windows.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
