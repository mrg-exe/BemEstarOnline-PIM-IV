using BemEstarOnline.Api.Dominio.Entidades;

namespace BemEstarOnline.Api.Dominio.Interfaces;

public record VendaDia(DateTime Dia, int QuantidadePedidos, decimal Faturamento);
public record ProdutoEstoqueBaixo(int IdMedicamento, string Nome, int Quantidade, int QuantidadeMinima);
public record ProdutoVencendo(int IdMedicamento, string Nome, DateTime ValidadeLote, int DiasRestantes);

/// <summary>
/// Relatórios gerenciais. No MySQL são atendidos pelas STORED PROCEDURES
/// (sp_relatorio_vendas, sp_produtos_estoque_baixo, sp_produtos_vencendo);
/// no modo de demonstração (banco em memória) o mesmo cálculo é feito em C#.
/// </summary>
public interface IRelatorioRepositorio
{
    Task<List<VendaDia>> VendasPorDiaAsync(DateTime inicio, DateTime fim);
    Task<List<ProdutoEstoqueBaixo>> EstoqueBaixoAsync();
    Task<List<ProdutoVencendo>> VencendoAsync(int dias);
}

/// <summary>
/// Efeitos automáticos que acontecem no banco depois de gravar um pedido.
/// No MySQL quem faz isso são os TRIGGERS (baixa de estoque, alerta de reposição,
/// histórico de status e devolução de estoque no cancelamento). No banco em memória
/// não existem triggers, então a implementação em memória reproduz as mesmas regras em C#.
/// </summary>
public interface IAutomacoesBanco
{
    /// <summary>Chamado logo após o pedido (com seus itens) ser gravado.</summary>
    Task AposCriarPedidoAsync(Pedido pedido);

    /// <summary>Chamado logo após o status do pedido ser alterado e gravado.</summary>
    Task AposAlterarStatusAsync(Pedido pedido, string statusAnterior);

    /// <summary>Indica se as regras rodam no próprio banco (true = triggers do MySQL).</summary>
    bool ExecutadasPeloBanco { get; }
}
