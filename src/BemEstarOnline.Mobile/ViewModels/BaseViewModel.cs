using CommunityToolkit.Mvvm.ComponentModel;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>Base de todas as telas: controla o "carregando" e mostra mensagens.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Livre))]
    private bool ocupado;

    public bool Livre => !Ocupado;

    protected static Task AvisarAsync(string mensagem, string titulo = "Bem Estar Online") =>
        Shell.Current.DisplayAlertAsync(titulo, mensagem, "OK");

    protected static Task<bool> ConfirmarAsync(string pergunta) =>
        Shell.Current.DisplayAlertAsync("Confirmar", pergunta, "Sim", "Não");
}
