using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity;

namespace BemEstarOnline.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        OnBackPressedDispatcher.AddCallback(this, new VoltarDoCelular(this));
    }

    // Botão "voltar" do celular: nas telas principais pergunta "Sair do app?"; nas outras deixa o MAUI voltar normalmente.
    private sealed class VoltarDoCelular : OnBackPressedCallback
    {
        private readonly MainActivity _tela;

        public VoltarDoCelular(MainActivity tela) : base(true) => _tela = tela;

        public override void HandleOnBackPressed()
        {
            if (Shell.Current is AppShell shell && shell.EstaNaTelaPrincipal)
            {
                _ = shell.PerguntarSeSaiAsync();
                return;
            }
            Enabled = false;
            _tela.OnBackPressedDispatcher.OnBackPressed();
            Enabled = true;
        }
    }
}
