using BemEstarOnline.Mobile.Modelos;
using SQLite;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// Banco SQLite dentro do celular. Guarda: (1) cópia do catálogo para consultar sem internet,
/// (2) o carrinho e (3) a fila de pedidos feitos offline, enviados depois pelo SincronizacaoServico.
/// Se o banco do celular falhar, o app NÃO fecha: o erro é guardado (RegistroDeErros) e o app segue só online.
/// </summary>
public class BancoLocalServico
{
    private SQLiteAsyncConnection? _conexao;
    private readonly SemaphoreSlim _abrir = new(1, 1);

    /// <summary>true quando o banco do celular deu erro (o catálogo avisa que o modo offline está desligado).</summary>
    public bool Indisponivel { get; private set; }

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

    /// <summary>Roda a operação; se o SQLite falhar, guarda o erro (só o primeiro) e devolve o valor padrão.</summary>
    private async Task<T> SeguroAsync<T>(Func<SQLiteAsyncConnection, Task<T>> operacao, T padrao)
    {
        try
        {
            return await operacao(await ConexaoAsync());
        }
        catch (Exception erro)
        {
            if (!Indisponivel) RegistroDeErros.Gravar(erro, "banco local do celular (SQLite)");
            Indisponivel = true;
            return padrao;
        }
    }

    private Task SeguroAsync(Func<SQLiteAsyncConnection, Task> operacao) =>
        SeguroAsync(async c => { await operacao(c); return true; }, false);

    // ---------- catálogo ----------
    public Task SalvarCatalogoAsync(IEnumerable<Produto> produtos) =>
        SeguroAsync(c => c.RunInTransactionAsync(t =>
        {
            t.DeleteAll<Produto>();
            t.InsertAll(produtos);
        }));

    public Task<List<Produto>> CatalogoAsync() =>
        SeguroAsync(c => c.Table<Produto>().OrderBy(p => p.Nome).ToListAsync(), new List<Produto>());

    // ---------- carrinho ----------
    public Task<List<ItemCarrinho>> CarrinhoAsync() =>
        SeguroAsync(c => c.Table<ItemCarrinho>().ToListAsync(), new List<ItemCarrinho>());

    public Task SalvarItemAsync(ItemCarrinho item) =>
        SeguroAsync(async c =>
        {
            if (item.Quantidade <= 0) await c.DeleteAsync<ItemCarrinho>(item.IdMedicamento);
            else await c.InsertOrReplaceAsync(item);
        });

    public Task LimparCarrinhoAsync() => SeguroAsync(c => c.DeleteAllAsync<ItemCarrinho>());

    // ---------- fila offline ----------
    public Task GuardarPedidoPendenteAsync(PedidoPendente pedido) => SeguroAsync(c => c.InsertAsync(pedido));

    public Task<List<PedidoPendente>> PendentesAsync(int idUsuario) =>
        SeguroAsync(c => c.Table<PedidoPendente>().Where(p => !p.Sincronizado && p.IdUsuario == idUsuario).ToListAsync(),
                    new List<PedidoPendente>());

    public Task AtualizarPendenteAsync(PedidoPendente pedido) => SeguroAsync(c => c.UpdateAsync(pedido));
}
