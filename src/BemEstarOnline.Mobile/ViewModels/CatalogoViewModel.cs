using System.Collections.ObjectModel;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>Catálogo com busca e categorias. Sem internet, usa a cópia guardada no SQLite.</summary>
public partial class CatalogoViewModel : BaseViewModel
{
    private readonly ApiServico _api;
    private readonly BancoLocalServico _banco;
    private readonly CarrinhoServico _carrinho;
    private List<Produto> _todos = new();

    public ObservableCollection<Produto> Produtos { get; } = new();
    public ObservableCollection<string> Categorias { get; } = new() { "Todas" };

    [ObservableProperty] private string busca = "";
    [ObservableProperty] private string categoriaSelecionada = "Todas";
    [ObservableProperty] private string aviso = "";
    [ObservableProperty] private bool temAviso;
    [ObservableProperty] private bool atualizando;

    public CatalogoViewModel(ApiServico api, BancoLocalServico banco, CarrinhoServico carrinho)
    {
        _api = api;
        _banco = banco;
        _carrinho = carrinho;
    }

    partial void OnBuscaChanged(string value) => Filtrar();
    partial void OnCategoriaSelecionadaChanged(string value) => Filtrar();

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Ocupado = true;
        try
        {
            _todos = await _api.GetAsync<List<Produto>>("produtos");
            await _banco.SalvarCatalogoAsync(_todos);
            TemAviso = false;
        }
        catch (ApiException)
        {
            _todos = await _banco.CatalogoAsync();
            Aviso = _todos.Count > 0
                ? "Sem conexão: mostrando o catálogo salvo no celular. Pedidos feitos agora serão enviados quando a internet voltar."
                : "Sem conexão e ainda sem catálogo salvo. Conecte-se uma vez para baixar os produtos.";
            TemAviso = true;
        }
        finally
        {
            Ocupado = false;
            Atualizando = false;
        }

        var nomes = _todos.Select(p => p.Categoria).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();
        Categorias.Clear();
        Categorias.Add("Todas");
        foreach (var n in nomes) Categorias.Add(n);
        if (!Categorias.Contains(CategoriaSelecionada)) CategoriaSelecionada = "Todas";
        Filtrar();
    }

    private void Filtrar()
    {
        var termo = Normalizar(Busca);
        var lista = _todos.Where(p =>
            (CategoriaSelecionada == "Todas" || p.Categoria == CategoriaSelecionada) &&
            (termo.Length == 0 || Normalizar($"{p.Nome} {p.PrincipioAtivo} {p.Fabricante} {p.Descricao}").Contains(termo)));
        Produtos.Clear();
        foreach (var p in lista) Produtos.Add(p);
    }

    [RelayCommand]
    private async Task AdicionarAsync(Produto produto)
    {
        if (!produto.Disponivel) { await AvisarAsync("Este produto está esgotado."); return; }
        await _carrinho.AdicionarAsync(produto);
        await AvisarAsync($"{produto.Nome} foi para o carrinho.");
    }

    [RelayCommand]
    private Task AbrirAsync(Produto produto) =>
        Shell.Current.GoToAsync("produto", new Dictionary<string, object> { ["Produto"] = produto });

    /// <summary>Mandar a foto da receita (RF39). Funciona sem login.</summary>
    [RelayCommand]
    private Task EnviarReceitaAsync() => Shell.Current.GoToAsync("enviar-receita");

    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var decomposto = texto.Normalize(System.Text.NormalizationForm.FormD);
        return new string(decomposto.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                                                System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
    }
}
