using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

public partial class ProdutoViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CarrinhoServico _carrinho;

    [ObservableProperty] private Produto? produto;
    [ObservableProperty] private int quantidade = 1;

    public ProdutoViewModel(CarrinhoServico carrinho) => _carrinho = carrinho;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Produto", out var p)) Produto = p as Produto;
        Quantidade = 1;
    }

    [RelayCommand] private void Mais() => Quantidade = Math.Min(99, Quantidade + 1);
    [RelayCommand] private void Menos() => Quantidade = Math.Max(1, Quantidade - 1);

    [RelayCommand]
    private async Task AdicionarAsync()
    {
        if (Produto is null) return;
        if (!Produto.Disponivel) { await AvisarAsync("Este produto está esgotado."); return; }
        await _carrinho.AdicionarAsync(Produto, Quantidade);
        await AvisarAsync($"{Quantidade}x {Produto.Nome} no carrinho.");
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Atalho para a assinatura (RF38), com este remédio já escolhido.</summary>
    [RelayCommand]
    private Task AssinarAsync() =>
        Produto is null ? Task.CompletedTask : Shell.Current.GoToAsync($"assinaturas?produto={Produto.IdMedicamento}");

    /// <summary>Lê o nome, a descrição e o preço em voz alta (acessibilidade).</summary>
    [RelayCommand]
    private async Task OuvirAsync()
    {
        if (Produto is null) return;
        var texto = $"{Produto.Nome}. {Produto.Apresentacao}. {Produto.Descricao}. Preço: {Produto.PrecoTexto}." +
                    (Produto.ExigeReceita ? " Precisa de receita médica." : "");
        try
        {
            var vozes = await TextToSpeech.Default.GetLocalesAsync();
            var portugues = vozes.FirstOrDefault(v => v.Language.StartsWith("pt", StringComparison.OrdinalIgnoreCase));
            await TextToSpeech.Default.SpeakAsync(texto, new SpeechOptions { Locale = portugues });
        }
        catch
        {
            await AvisarAsync("A leitura em voz alta não está disponível neste aparelho.");
        }
    }
}
