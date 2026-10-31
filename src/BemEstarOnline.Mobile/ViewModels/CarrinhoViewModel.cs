using System.Collections.ObjectModel;
using System.Text.Json;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>Carrinho e finalização. Sem internet, o pedido vai para a fila e é enviado depois.</summary>
public partial class CarrinhoViewModel : BaseViewModel
{
    private readonly CarrinhoServico _carrinho;
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly BancoLocalServico _banco;
    private readonly Armazenamento _armazenamento;

    public ObservableCollection<ItemCarrinho> Itens { get; } = new();
    public ObservableCollection<Endereco> Enderecos { get; } = new();
    public List<string> FormasPagamento { get; } = new() { "Pix", "Cartão de crédito", "Cartão de débito", "Dinheiro na entrega" };
    private static readonly string[] CodigosPagamento = { "PIX", "CARTAO_CREDITO", "CARTAO_DEBITO", "DINHEIRO" };

    [ObservableProperty] private Endereco? enderecoSelecionado;
    [ObservableProperty] private int pagamentoSelecionado;
    [ObservableProperty] private string cupom = "";
    [ObservableProperty] private string observacao = "";
    [ObservableProperty] private string subtotalTexto = "R$ 0,00";
    [ObservableProperty] private string descontoTexto = "";
    [ObservableProperty] private string freteTexto = "";
    [ObservableProperty] private string totalTexto = "R$ 0,00";
    [ObservableProperty] private string mensagem = "";
    [ObservableProperty] private bool vazio = true;
    [ObservableProperty] private bool requerReceita;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Logado))]
    private bool precisaLogin;

    public bool Logado => !PrecisaLogin;

    public CarrinhoViewModel(CarrinhoServico carrinho, ApiServico api, SessaoServico sessao, BancoLocalServico banco, Armazenamento armazenamento)
    {
        _carrinho = carrinho;
        _api = api;
        _sessao = sessao;
        _banco = banco;
        _armazenamento = armazenamento;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        var itens = await _carrinho.ItensAsync();
        Itens.Clear();
        foreach (var i in itens.OrderBy(i => i.Nome)) Itens.Add(i);
        Vazio = Itens.Count == 0;
        PrecisaLogin = !_sessao.Logado;
        RequerReceita = Itens.Any(i => i.ExigeReceita);
        await CarregarEnderecosAsync();
        await CalcularAsync();
    }

    private async Task CarregarEnderecosAsync()
    {
        if (!_sessao.Logado) return;
        List<Endereco> lista;
        try
        {
            lista = await _api.GetAsync<List<Endereco>>("enderecos");
            _armazenamento.Gravar("enderecos", JsonSerializer.Serialize(lista)); // para usar offline
        }
        catch (ApiException)
        {
            var salvo = _armazenamento.Ler("enderecos");
            lista = salvo is null ? new() : JsonSerializer.Deserialize<List<Endereco>>(salvo) ?? new();
        }
        var idAntes = EnderecoSelecionado?.IdEndereco;
        Enderecos.Clear();
        foreach (var e in lista) Enderecos.Add(e);
        // Voltando de "Novo endereço": já deixa escolhido o que acabou de ser cadastrado.
        var novo = _idsAntesDoNovo is null ? null : Enderecos.FirstOrDefault(e => !_idsAntesDoNovo.Contains(e.IdEndereco));
        _idsAntesDoNovo = null;
        EnderecoSelecionado = novo
            ?? Enderecos.FirstOrDefault(e => e.IdEndereco == idAntes)
            ?? Enderecos.FirstOrDefault(e => e.Principal) ?? Enderecos.FirstOrDefault();
    }

    private HashSet<int>? _idsAntesDoNovo;

    /// <summary>Cadastrar endereço sem sair da compra (RF37): abre "Meus endereços" e volta para cá ao salvar.</summary>
    [RelayCommand]
    private Task NovoEnderecoAsync()
    {
        _idsAntesDoNovo = Enderecos.Select(e => e.IdEndereco).ToHashSet();
        return Shell.Current.GoToAsync("enderecos?carrinho=true");
    }

    [RelayCommand]
    private async Task CalcularAsync()
    {
        if (Itens.Count == 0)
        {
            SubtotalTexto = TotalTexto = Texto.Moeda(0);
            DescontoTexto = FreteTexto = Mensagem = "";
            return;
        }
        try
        {
            var r = await _api.PostAsync<ResumoValores>("pedidos/simular", new
            {
                itens = Itens.Select(i => new ItemQuantidade { IdMedicamento = i.IdMedicamento, Quantidade = i.Quantidade }),
                codigoCupom = string.IsNullOrWhiteSpace(Cupom) ? null : Cupom.Trim().ToUpperInvariant()
            });
            SubtotalTexto = Texto.Moeda(r.Subtotal);
            DescontoTexto = r.Desconto > 0 ? $"− {Texto.Moeda(r.Desconto)}" : "";
            FreteTexto = r.Frete == 0 ? "Grátis" : Texto.Moeda(r.Frete);
            TotalTexto = Texto.Moeda(r.Total);
            Mensagem = r.MensagemCupom ?? (r.FaltaParaFreteGratis > 0 ? $"Faltam {Texto.Moeda(r.FaltaParaFreteGratis)} para o frete grátis." : "");
        }
        catch (ApiException ex)
        {
            // Sem internet: cálculo aproximado no próprio celular (o servidor recalcula ao receber).
            var subtotal = Itens.Sum(i => i.Preco * i.Quantidade);
            var frete = subtotal >= 99 ? 0 : 8.90m;
            SubtotalTexto = Texto.Moeda(subtotal);
            DescontoTexto = "";
            FreteTexto = frete == 0 ? "Grátis" : Texto.Moeda(frete);
            TotalTexto = Texto.Moeda(subtotal + frete);
            Mensagem = ex.SemConexao ? "Sem conexão: valores aproximados, confirmados no envio." : ex.Message;
        }
    }

    [RelayCommand]
    private async Task MaisAsync(ItemCarrinho item) { await _carrinho.AlterarAsync(item, item.Quantidade + 1); await CarregarAsync(); }

    [RelayCommand]
    private async Task MenosAsync(ItemCarrinho item) { await _carrinho.AlterarAsync(item, item.Quantidade - 1); await CarregarAsync(); }

    [RelayCommand]
    private async Task RemoverAsync(ItemCarrinho item) { await _carrinho.AlterarAsync(item, 0); await CarregarAsync(); }

    [RelayCommand]
    private Task EntrarAsync() => Shell.Current.GoToAsync("//login");

    [RelayCommand]
    private async Task FinalizarAsync()
    {
        if (Itens.Count == 0) return;
        if (!_sessao.Logado) { await Shell.Current.GoToAsync("//login"); return; }
        if (EnderecoSelecionado is null)
        {
            if (await ConfirmarAsync("Você ainda não escolheu um endereço de entrega. Cadastrar um agora?"))
                await NovoEnderecoAsync();
            return;
        }

        var pedido = new PedidoCriar
        {
            Itens = Itens.Select(i => new ItemQuantidade { IdMedicamento = i.IdMedicamento, Quantidade = i.Quantidade }).ToList(),
            IdEndereco = EnderecoSelecionado.IdEndereco,
            CodigoCupom = string.IsNullOrWhiteSpace(Cupom) ? null : Cupom.Trim().ToUpperInvariant(),
            MetodoPagamento = CodigosPagamento[Math.Clamp(PagamentoSelecionado, 0, CodigosPagamento.Length - 1)],
            Observacao = string.IsNullOrWhiteSpace(Observacao) ? null : Observacao.Trim(),
            Canal = "APP"
        };

        Ocupado = true;
        try
        {
            var criado = await _api.PostAsync<PedidoDetalhe>("pedidos", pedido);
            await _carrinho.LimparAsync();
            Cupom = Observacao = "";
            await CarregarAsync();
            await AvisarAsync($"Pedido nº {criado.IdPedido} recebido! Acompanhe na aba Pedidos.");
            await Shell.Current.GoToAsync($"//pedidos");
        }
        catch (ApiException ex) when (ex.SemConexao)
        {
            await _banco.GuardarPedidoPendenteAsync(new PedidoPendente
            {
                IdUsuario = _sessao.Usuario?.IdUsuario ?? 0,
                JsonPedido = JsonSerializer.Serialize(pedido)
            });
            await _carrinho.LimparAsync();
            await CarregarAsync();
            await AvisarAsync("Você está sem internet. O pedido ficou guardado no celular e será enviado sozinho quando a conexão voltar.");
        }
        catch (ApiException ex)
        {
            await AvisarAsync(ex.Message);
        }
        finally { Ocupado = false; }
    }
}
