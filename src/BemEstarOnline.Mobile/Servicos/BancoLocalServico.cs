using BemEstarOnline.Mobile.Modelos;
using SQLite;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// Banco SQLite dentro do celular. Guarda: (1) cópia do catálogo para consultar sem internet,
/// (2) o carrinho e (3) a fila de pedidos feitos offline, enviados depois pelo SincronizacaoServico.
/// </summary>
public class BancoLocalServico
{
    private SQLiteAsyncConnection? _conexao;
    private readonly SemaphoreSlim _abrir = new(1, 1);

    private async Task<SQLiteAsyncConnection> ConexaoAsync()
    {
        if (_conexao is not null) return _conexao;
        await _abrir.WaitAsync();
        try
        {
            if (_conexao is null)
            {
                var caminho = Path.Combine(FileSystem.AppDataDirectory, "bemestar_local.db3");
                var conexao = new SQLiteAsyncConnection(caminho);
                await conexao.CreateTableAsync<Produto>();
                await conexao.CreateTableAsync<ItemCarrinho>();
                await conexao.CreateTableAsync<PedidoPendente>();
                _conexao = conexao;
            }
            return _conexao;
        }
        finally { _abrir.Release(); }
    }

    // ---------- catálogo ----------
    public async Task SalvarCatalogoAsync(IEnumerable<Produto> produtos)
    {
        var c = await ConexaoAsync();
        await c.RunInTransactionAsync(t =>
        {
            t.DeleteAll<Produto>();
            t.InsertAll(produtos);
        });
    }

    public async Task<List<Produto>> CatalogoAsync() =>
        await (await ConexaoAsync()).Table<Produto>().OrderBy(p => p.Nome).ToListAsync();

    // ---------- carrinho ----------
    public async Task<List<ItemCarrinho>> CarrinhoAsync() =>
        await (await ConexaoAsync()).Table<ItemCarrinho>().ToListAsync();

    public async Task SalvarItemAsync(ItemCarrinho item)
    {
        var c = await ConexaoAsync();
        if (item.Quantidade <= 0) await c.DeleteAsync<ItemCarrinho>(item.IdMedicamento);
        else await c.InsertOrReplaceAsync(item);
    }

    public async Task LimparCarrinhoAsync() => await (await ConexaoAsync()).DeleteAllAsync<ItemCarrinho>();

    // ---------- fila offline ----------
    public async Task GuardarPedidoPendenteAsync(PedidoPendente pedido) => await (await ConexaoAsync()).InsertAsync(pedido);

    public async Task<List<PedidoPendente>> PendentesAsync(int idUsuario) =>
        await (await ConexaoAsync()).Table<PedidoPendente>().Where(p => !p.Sincronizado && p.IdUsuario == idUsuario).ToListAsync();

    public async Task AtualizarPendenteAsync(PedidoPendente pedido) => await (await ConexaoAsync()).UpdateAsync(pedido);
}
