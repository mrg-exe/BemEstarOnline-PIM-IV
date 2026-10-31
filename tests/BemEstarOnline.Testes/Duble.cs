using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Testes;

/*
 * "Dublês" (fakes): versões de mentira do banco e das automações, só para os testes.
 * Assim o serviço é testado sozinho, sem MySQL, e dá para conferir o que ele pediu para gravar.
 */

public class PedidosFalsos : IPedidoRepositorio
{
    public List<Pedido> Pedidos { get; } = new();

    public Task<Pedido?> ObterDetalheAsync(int idPedido) => Task.FromResult(Pedidos.FirstOrDefault(p => p.IdPedido == idPedido));
    public Task<List<Pedido>> ListarPorUsuarioAsync(int idUsuario) => Task.FromResult(Pedidos.Where(p => p.IdUsuario == idUsuario).ToList());
    public Task<List<Pedido>> ListarAsync(string? status, int limite) => Task.FromResult(Pedidos.Take(limite).ToList());
    public Task<bool> ExisteAlgumAsync() => Task.FromResult(Pedidos.Count > 0);
    public void Adicionar(Pedido pedido) => Pedidos.Add(pedido);
    public Task<List<Pedido>> ListarParaEntregadorAsync(int idEntregador) => Task.FromResult(Pedidos.ToList());
    public Task<List<(int idUsuario, int quantidade, decimal total)>> ResumoPorClienteAsync() => Task.FromResult(new List<(int, int, decimal)>());
    public Task<(int pedidosHoje, decimal vendasHoje, decimal vendasMes, int emAberto)> IndicadoresAsync() => Task.FromResult((0, 0m, 0m, 0));
}

public class AutomacoesFalsas : IAutomacoesBanco
{
    /// <summary>Cada mudança de status avisada (o que no MySQL vira o trigger do histórico).</summary>
    public List<(int idPedido, string anterior, string novo)> Avisos { get; } = new();

    public Task AposCriarPedidoAsync(Pedido pedido) => Task.CompletedTask;
    public Task AposAlterarStatusAsync(Pedido pedido, string statusAnterior)
    {
        Avisos.Add((pedido.IdPedido, statusAnterior, pedido.Status));
        return Task.CompletedTask;
    }
    public bool ExecutadasPeloBanco => false;
}

public class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int VezesQueSalvou { get; private set; }
    public Task SalvarAsync() { VezesQueSalvou++; return Task.CompletedTask; }
}

public class DeliveryFalso : IDeliveryRepositorio
{
    public List<DeliverySolicitacao> Solicitacoes { get; } = new();

    public Task<List<DeliverySolicitacao>> ListarPorUsuarioAsync(int idUsuario) => Task.FromResult(Solicitacoes.Where(s => s.IdUsuario == idUsuario).ToList());
    public Task<List<DeliverySolicitacao>> ListarParaAlterarAsync(int idUsuario) => ListarPorUsuarioAsync(idUsuario);
    public Task<List<DeliverySolicitacao>> ListarTodasAsync() => Task.FromResult(Solicitacoes.ToList());
    public Task<DeliverySolicitacao?> ObterPorIdAsync(int id) => Task.FromResult(Solicitacoes.FirstOrDefault(s => s.IdDelivery == id));
    public Task<int> ContarAbertasAsync() => Task.FromResult(Solicitacoes.Count);
    public void Adicionar(DeliverySolicitacao solicitacao)
    {
        solicitacao.IdDelivery = Solicitacoes.Count + 1;
        Solicitacoes.Add(solicitacao);
    }
}

/// <summary>Pasta privada de mentira: guarda os arquivos na memória.</summary>
public class ArquivosFalsos : IArquivosPrivados
{
    public Dictionary<string, byte[]> Guardados { get; } = new();

    public async Task<string> GuardarReceitaAsync(Stream conteudo, string extensao)
    {
        var copia = new MemoryStream();
        await conteudo.CopyToAsync(copia);
        var nome = $"{Guid.NewGuid():N}{extensao}";
        Guardados[nome] = copia.ToArray();
        return nome;
    }

    public ArquivoGuardado? AbrirReceita(string nome) =>
        Guardados.TryGetValue(nome, out var bytes) ? new ArquivoGuardado(new MemoryStream(bytes), "image/jpeg") : null;

    public void ApagarReceita(string nome) => Guardados.Remove(nome);
}
