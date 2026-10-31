using System.ComponentModel;
using BemEstarOnline.Mobile.ViewModels;

namespace BemEstarOnline.Mobile.Views;

public partial class TextoLegalPage : ContentPage
{
    private readonly TextoLegalViewModel _vm;

    public TextoLegalPage(TextoLegalViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += AoMudar;
    }

    private void AoMudar(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextoLegalViewModel.Blocos)) Montar();
    }

    /// <summary>Transforma cada bloco num Label (os títulos viram cabeçalhos para o leitor de tela).</summary>
    private void Montar()
    {
        Conteudo.Children.Clear();
        foreach (var bloco in _vm.Blocos)
        {
            var texto = new Label { FormattedText = Formatar(bloco.Trechos) };
            switch (bloco.Tipo)
            {
                case TipoBloco.Titulo:
                    texto.Style = (Style)Application.Current!.Resources["Titulo"];
                    texto.FontSize = 26;
                    SemanticProperties.SetHeadingLevel(texto, SemanticHeadingLevel.Level1);
                    Conteudo.Children.Add(texto);
                    break;
                case TipoBloco.Secao:
                    texto.Style = (Style)Application.Current!.Resources["Titulo"];
                    texto.FontSize = 19;
                    texto.Margin = new Thickness(0, 12, 0, 0);
                    SemanticProperties.SetHeadingLevel(texto, SemanticHeadingLevel.Level2);
                    Conteudo.Children.Add(texto);
                    break;
                case TipoBloco.Nota:
                    texto.Style = (Style)Application.Current!.Resources["TextoSuave"];
                    texto.FontSize = 14;
                    Conteudo.Children.Add(texto);
                    break;
                case TipoBloco.Item:
                    var linha = new Grid { ColumnDefinitions = { new ColumnDefinition(18), new ColumnDefinition(GridLength.Star) } };
                    var marcador = new Label { Text = "•", FontAttributes = FontAttributes.Bold };
                    AutomationProperties.SetIsInAccessibleTree(marcador, false); // o leitor de tela não lê "bolinha"
                    linha.Add(marcador);
                    linha.Add(texto, 1);
                    Conteudo.Children.Add(linha);
                    break;
                default:
                    Conteudo.Children.Add(texto);
                    break;
            }
        }
    }

    private static FormattedString Formatar(IEnumerable<Trecho> trechos)
    {
        var f = new FormattedString();
        foreach (var t in trechos)
        {
            var span = new Span { Text = t.Texto };
            // Só marca o que é negrito/itálico; o resto herda do Label (assim os títulos continuam em negrito).
            if (t.Negrito || t.Italico)
                span.FontAttributes = (t.Negrito ? FontAttributes.Bold : FontAttributes.None) | (t.Italico ? FontAttributes.Italic : FontAttributes.None);
            f.Spans.Add(span);
        }
        return f;
    }
}
