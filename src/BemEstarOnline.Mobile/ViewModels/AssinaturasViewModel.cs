using System.Collections.ObjectModel;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Assinaturas de uso contínuo (RF38), igual ao site (assinatura.js e Minha conta): escolher o remédio,
/// o paciente, as caixas e o intervalo, ver o valor com 10% de desconto; listar, pausar, retomar e cancelar.
/// Aberta pelo Perfil ou pelo atalho "Assinar" de um produto de uso contínuo (já vem com ele escolhido).
/// </summary>
public partial class AssinaturasViewModel : BaseViewModel, IQueryAttributable
{
    private const decimal Desconto = 0.10m;
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly BancoLocalServico _banco;
    private int? _idPreEscolhido;

    public ObservableCollection<Produto> Produtos { get; } = new();
    public ObservableCollection<Assinatura> Assinaturas { get; } = new();
    public List<string> OpcoesQuantidade { get; } = new() { "1 caixa", "2 caixas", "3 caixas", "4 caixas" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Visitante))]
    private bool logado;

    public bool Visitante => !Logado;
    [ObservableProperty] private bool semAssinaturas;
    [ObservableProperty] private Produto? produtoEscolhido;
    [ObservableProperty] private int indiceQuantidade;
    [ObservableProperty] private string periodo = "30";
    [ObservableProperty] private string paciente = "";
    [ObservableProperty] private bool temValor;
    [ObservableProperty] private string precoNormalTexto = "";
    [ObservableProperty] private string porEntregaTexto = "";
    [ObservableProperty] private string economiaTexto = "Escolha um medicamento para ver o valor.";

    public AssinaturasViewModel(ApiServico api, SessaoServico sessao, BancoLocalServico banco)
    {
        _api = api;
        _sessao = sessao;
        _banco = banco;
        WeakReferenceMessenger.Default.Register<AssinaturasViewModel, AssinaturasMudaram>(this, (vm, m) =>
        {
            if (m.Origem != vm && vm.Logado) _ = vm.ListarAsync();
        });
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("produto", out var id) && int.TryParse(id?.ToString(), out var numero)) _idPreEscolhido = numero;
    }

    partial void OnProdutoEscolhidoChanged(Produto? value) => AtualizarValor();
    partial void OnIndiceQuantidadeChanged(int value) => AtualizarValor();

    private int Quantidade => Math.Clamp(IndiceQuantidade, 0, 3) + 1;

    private void AtualizarValor()
    {
        TemValor = ProdutoEscolhido is not null;
        if (ProdutoEscolhido is null) { EconomiaTexto = "Escolha um medicamento para ver o valor."; return; }
        var cheio = ProdutoEscolhido.Preco * Quantidade;
        var comDesconto = Math.Round(cheio * (1 - Desconto), 2);
        PrecoNormalTexto = Texto.Moeda(cheio);
        PorEntregaTexto = Texto.Moeda(comDesconto);
        EconomiaTexto = $"Você economiza {Texto.Moeda(cheio - comDesconto)} a cada entrega. Primeira entrega: amanhã.";
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Logado = _sessao.Logado;
        if (!Logado) return;
        if (string.IsNullOrWhiteSpace(Paciente)) Paciente = _sessao.Usuario?.NomeExibicao ?? "";

        List<Produto> lista;
        try { lista = await _api.GetAsync<List<Produto>>("produtos?usoContinuo=true"); }
        catch (ApiException) { lista = (await _banco.CatalogoAsync()).Where(p => p.UsoContinuo).ToList(); }
        var idAtual = _idPreEscolhido ?? ProdutoEscolhido?.IdMedicamento;
        _idPreEscolhido = null;
        Produtos.Clear();
        foreach (var p in lista.Where(p => p.Disponivel).OrderBy(p => p.Nome)) Produtos.Add(p);
        ProdutoEscolhido = Produtos.FirstOrDefault(p => p.IdMedicamento == idAtual);

        await ListarAsync();
    }

    private async Task ListarAsync()
    {
        try
        {
            var lista = await _api.GetAsync<List<Assinatura>>("assinaturas");
            Assinaturas.Clear();
            // Ativas primeiro, depois pausadas; canceladas no fim.
            foreach (var a in lista.OrderBy(a => a.Status switch { "ATIVA" => 0, "PAUSADA" => 1, _ => 2 }).ThenByDescending(a => a.IdAssinatura))
                Assinaturas.Add(a);
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        SemAssinaturas = Assinaturas.Count == 0;
    }

    [RelayCommand]
    private Task EntrarAsync() => Shell.Current.GoToAsync("//login");

    [RelayCommand]
    private async Task CriarAsync()
    {
        if (ProdutoEscolhido is null) { await AvisarAsync("Escolha o medicamento."); return; }
        if (string.IsNullOrWhiteSpace(Paciente)) { await AvisarAsync("Digite o nome do paciente."); return; }
        Ocupado = true;
        try
        {
            var a = await _api.PostAsync<Assinatura>("assinaturas", new
            {
                idMedicamento = ProdutoEscolhido.IdMedicamento,
                nomePaciente = Paciente.Trim(),
                quantidade = Quantidade,
                periodicidadeDias = int.TryParse(Periodo, out var dias) ? dias : 30
            });
            await ListarAsync();
            WeakReferenceMessenger.Default.Send(new AssinaturasMudaram(this));
            await AvisarAsync($"Assinatura criada! Primeira entrega em {Texto.Data(a.ProximaEntrega)}.");
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        finally { Ocupado = false; }
    }

    [RelayCommand] private Task PausarAsync(Assinatura a) => MudarStatusAsync(a, "PAUSADA");
    [RelayCommand] private Task RetomarAsync(Assinatura a) => MudarStatusAsync(a, "ATIVA");

    [RelayCommand]
    private async Task CancelarAsync(Assinatura a)
    {
        if (await ConfirmarAsync($"Cancelar a assinatura de {a.Medicamento}?")) await MudarStatusAsync(a, "CANCELADA");
    }

    private async Task MudarStatusAsync(Assinatura a, string status)
    {
        try
        {
            await _api.PatchAsync<Assinatura>($"assinaturas/{a.IdAssinatura}/status", new { status });
            await ListarAsync();
            WeakReferenceMessenger.Default.Send(new AssinaturasMudaram(this));
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }
}
