using BemEstarOnline.Mobile.Modelos;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>Carrinho do app: fica no SQLite e, se a pessoa estiver logada, é copiado para a API (mesmo carrinho do site).</summary>
public class CarrinhoServico
{
    private readonly BancoLocalServico _banco;
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;

    public event EventHandler? Mudou;

    public CarrinhoServico(BancoLocalServico banco, ApiServico api, SessaoServico sessao)
    {
        _banco = banco;
        _api = api;
        _sessao = sessao;
    }

    public Task<List<ItemCarrinho>> ItensAsync() => _banco.CarrinhoAsync();

    public async Task AdicionarAsync(Produto produto, int quantidade = 1)
    {
        var itens = await _banco.CarrinhoAsync();
        var item = itens.FirstOrDefault(i => i.IdMedicamento == produto.IdMedicamento)
                   ?? new ItemCarrinho { IdMedicamento = produto.IdMedicamento, Nome = produto.Nome, ExigeReceita = produto.ExigeReceita };
        item.Preco = produto.Preco;
        item.Quantidade = Math.Min(99, item.Quantidade + quantidade);
        await _banco.SalvarItemAsync(item);
        await AposMudarAsync();
    }

    public async Task AlterarAsync(ItemCarrinho item, int novaQuantidade)
    {
        item.Quantidade = Math.Min(99, novaQuantidade);
        await _banco.SalvarItemAsync(item);
        await AposMudarAsync();
    }

    public async Task LimparAsync()
    {
        await _banco.LimparCarrinhoAsync();
        await AposMudarAsync();
    }

    /// <summary>Depois do login: se o carrinho do celular estiver vazio, traz o que foi salvo no site.</summary>
    public async Task SincronizarAoEntrarAsync()
    {
        try
        {
            var locais = await _banco.CarrinhoAsync();
            if (locais.Count == 0)
            {
                var servidor = await _api.GetAsync<CarrinhoServidor>("carrinho");
                foreach (var i in servidor.Itens)
                    await _banco.SalvarItemAsync(new ItemCarrinho
                    {
                        IdMedicamento = i.IdMedicamento, Nome = i.Nome, Preco = i.PrecoUnitario, Quantidade = i.Quantidade, ExigeReceita = i.ExigeReceita
                    });
                Mudou?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                await EnviarParaServidorAsync(locais);
            }
        }
        catch { /* o carrinho local continua valendo */ }
    }

    private async Task AposMudarAsync()
    {
        Mudou?.Invoke(this, EventArgs.Empty);
        if (_sessao.Logado)
        {
            try { await EnviarParaServidorAsync(await _banco.CarrinhoAsync()); } catch { /* offline: tudo bem */ }
        }
    }

    private Task EnviarParaServidorAsync(List<ItemCarrinho> itens) =>
        _api.PutAsync<object>("carrinho", itens.Select(i => new ItemQuantidade { IdMedicamento = i.IdMedicamento, Quantidade = i.Quantidade }).ToList());

    private class CarrinhoServidor
    {
        public List<ItemServidor> Itens { get; set; } = new();
    }

    private class ItemServidor
    {
        public int IdMedicamento { get; set; }
        public string Nome { get; set; } = "";
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public bool ExigeReceita { get; set; }
    }
}
