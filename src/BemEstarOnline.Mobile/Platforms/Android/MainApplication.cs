using Android.App;
using Android.Runtime;

namespace BemEstarOnline.Mobile;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
    {
        // No Android, erro sem tratamento passa primeiro por aqui: guarda a mensagem antes de o app fechar.
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
            BemEstarOnline.Mobile.Servicos.RegistroDeErros.Gravar(e.Exception, "o app fechou sozinho (Android)");
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
