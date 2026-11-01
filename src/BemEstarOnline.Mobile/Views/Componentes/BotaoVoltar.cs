namespace BemEstarOnline.Mobile.Views.Componentes;

/// <summary>
/// Botão "Voltar" bem visível no topo das telas internas. No Windows a seta de voltar da barra de título é pequena e
/// clara demais (difícil de achar); no Android ela já aparece na barra verde e o celular tem o botão de voltar,
/// então lá este botão fica escondido.
/// </summary>
public class BotaoVoltar : Button
{
    /// <summary>Use false quando a tela não deixa voltar (ex.: troca de senha obrigatória).</summary>
    public static readonly BindableProperty PermitidoProperty = BindableProperty.Create(
        nameof(Permitido), typeof(bool), typeof(BotaoVoltar), true,
        propertyChanged: (b, _, _) => ((BotaoVoltar)b).Atualizar());

    public bool Permitido
    {
        get => (bool)GetValue(PermitidoProperty);
        set => SetValue(PermitidoProperty, value);
    }

    public BotaoVoltar()
    {
        Text = "←  Voltar";
        if (Application.Current?.Resources.TryGetValue("BotaoVoltar", out var estilo) == true) Style = (Style)estilo;
        SemanticProperties.SetDescription(this, "Voltar para a tela anterior");
        Clicked += async (_, _) => await Shell.Current.GoToAsync("..");
        Atualizar();
    }

    private void Atualizar() => IsVisible = Permitido && DeviceInfo.Platform == DevicePlatform.WinUI;
}
