using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;

namespace BemEstarOnline.Testes;

/// <summary>Fluxo de status do pedido (Pedido.AlterarStatus) e regras ligadas a ele.</summary>
public class PedidoTestes
{
    private static Pedido PedidoEm(string status, string? statusPagamento = null)
    {
        var pedido = new Pedido { IdPedido = 1, Status = status };
        if (statusPagamento is not null) pedido.Pagamentos.Add(new Pagamento { IdPagamento = 1, Status = statusPagamento });
        return pedido;
    }

    [Theory]
    [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.Pago)]
    [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.EmSeparacao)] // dinheiro na entrega: separa antes de pagar
    [InlineData(StatusPedido.Pago, StatusPedido.EmSeparacao)]
    [InlineData(StatusPedido.EmSeparacao, StatusPedido.SaiuParaEntrega)]
    [InlineData(StatusPedido.SaiuParaEntrega, StatusPedido.Entregue)]
    [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.Cancelado)]
    [InlineData(StatusPedido.Pago, StatusPedido.Cancelado)]
    [InlineData(StatusPedido.EmSeparacao, StatusPedido.Cancelado)]
    public void Muda_de_status_quando_o_fluxo_permite(string atual, string novo)
    {
        var pedido = PedidoEm(atual);

        var anterior = pedido.AlterarStatus(novo);

        Assert.Equal(novo, pedido.Status);
        Assert.Equal(atual, anterior); // devolve o status de antes (usado no histórico)
        Assert.NotNull(pedido.AtualizadoEm);
    }

    [Theory]
    [InlineData(StatusPedido.Entregue, StatusPedido.Cancelado)]        // entregue não volta atrás
    [InlineData(StatusPedido.Cancelado, StatusPedido.Pago)]            // cancelado não reabre
    [InlineData(StatusPedido.SaiuParaEntrega, StatusPedido.Cancelado)] // já saiu: não cancela no meio do caminho
    [InlineData(StatusPedido.Pago, StatusPedido.Entregue)]             // não pula etapas
    [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.SaiuParaEntrega)]
    [InlineData(StatusPedido.EmSeparacao, StatusPedido.Pago)]          // não anda para trás
    public void Recusa_mudanca_fora_do_fluxo_e_mantem_o_status(string atual, string novo)
    {
        var pedido = PedidoEm(atual);

        var erro = Assert.Throws<RegraDeNegocioException>(() => pedido.AlterarStatus(novo));

        Assert.Contains("Não é possível mudar", erro.Message);
        Assert.Equal(atual, pedido.Status);
    }

    [Fact]
    public void Recusa_status_que_nao_existe()
    {
        var pedido = PedidoEm(StatusPedido.Pago);

        Assert.Throws<RegraDeNegocioException>(() => pedido.AlterarStatus("VOANDO"));
        Assert.Equal(StatusPedido.Pago, pedido.Status);
    }

    [Fact]
    public void Cancelar_pedido_ja_pago_estorna_o_pagamento()
    {
        var pedido = PedidoEm(StatusPedido.Pago, StatusPagamento.Aprovado);

        pedido.AlterarStatus(StatusPedido.Cancelado);

        Assert.Equal(StatusPagamento.Estornado, pedido.Pagamentos.Single().Status);
    }

    [Fact]
    public void Entregar_pedido_pago_na_entrega_aprova_o_pagamento()
    {
        var pedido = PedidoEm(StatusPedido.SaiuParaEntrega, StatusPagamento.Pendente);

        pedido.AlterarStatus(StatusPedido.Entregue);

        var pagamento = pedido.Pagamentos.Single();
        Assert.Equal(StatusPagamento.Aprovado, pagamento.Status);
        Assert.NotNull(pagamento.PagoEm);
    }

    [Theory]
    [InlineData(StatusPedido.AguardandoPagamento, true)]
    [InlineData(StatusPedido.Pago, true)]
    [InlineData(StatusPedido.EmSeparacao, false)]
    [InlineData(StatusPedido.SaiuParaEntrega, false)]
    [InlineData(StatusPedido.Entregue, false)]
    public void Cliente_so_cancela_antes_da_separacao(string status, bool podeCancelar) =>
        Assert.Equal(podeCancelar, PedidoEm(status).ClientePodeCancelar);

    [Fact]
    public void Entregador_so_pega_pedido_em_separacao()
    {
        var pedido = PedidoEm(StatusPedido.Pago);

        Assert.Throws<RegraDeNegocioException>(() => pedido.AtribuirEntregador(7));
        Assert.Null(pedido.IdEntregador);
    }

    [Fact]
    public void Dois_entregadores_nao_pegam_o_mesmo_pedido()
    {
        var pedido = PedidoEm(StatusPedido.EmSeparacao);
        pedido.AtribuirEntregador(7);

        var erro = Assert.Throws<RegraDeNegocioException>(() => pedido.AtribuirEntregador(8));

        Assert.Contains("Outro entregador", erro.Message);
        Assert.Equal(7, pedido.IdEntregador);
    }
}
