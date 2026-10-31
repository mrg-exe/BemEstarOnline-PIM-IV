using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Servicos;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;

namespace BemEstarOnline.Testes;

/// <summary>
/// Confirmação da entrega pelo entregador (EntregaServico.ConfirmarEntregaAsync):
/// remédio de tarja vermelha só é entregue com a receita conferida.
/// </summary>
public class EntregaServicoTestes
{
    private const int Entregador = 7;
    private readonly PedidosFalsos _pedidos = new();
    private readonly AutomacoesFalsas _automacoes = new();
    private readonly UnidadeDeTrabalhoFalsa _uow = new();
    private readonly EntregaServico _servico;

    public EntregaServicoTestes() => _servico = new EntregaServico(_pedidos, _automacoes, _uow);

    private Pedido PedidoNaRua(bool requerReceita, int idEntregador = Entregador, string status = StatusPedido.SaiuParaEntrega)
    {
        var pedido = new Pedido
        {
            IdPedido = 10,
            Status = status,
            RequerReceita = requerReceita,
            IdEntregador = idEntregador,
            Endereco = new Endereco { Logradouro = "Rua das Flores", Numero = "120", Latitude = -23.36, Longitude = -46.87 }
        };
        _pedidos.Adicionar(pedido);
        return pedido;
    }

    [Fact]
    public async Task Pedido_com_receita_nao_e_entregue_sem_conferir_a_receita()
    {
        var pedido = PedidoNaRua(requerReceita: true);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.ConfirmarEntregaAsync(10, Entregador, new EntregarDto(ReceitaConferida: false)));

        Assert.Contains("Confira a receita", erro.Message);
        Assert.Equal(StatusPedido.SaiuParaEntrega, pedido.Status); // continua na rua
        Assert.False(pedido.ReceitaConferida);
        Assert.Equal(0, _uow.VezesQueSalvou);                     // nada foi gravado
        Assert.Empty(_automacoes.Avisos);                          // nem histórico
    }

    [Fact]
    public async Task Pedido_com_receita_conferida_e_entregue()
    {
        var pedido = PedidoNaRua(requerReceita: true);

        var resultado = await _servico.ConfirmarEntregaAsync(10, Entregador, new EntregarDto(ReceitaConferida: true));

        Assert.Equal(StatusPedido.Entregue, pedido.Status);
        Assert.True(pedido.ReceitaConferida);
        Assert.True(resultado.ReceitaConferida);
        Assert.Equal(1, _uow.VezesQueSalvou);
        Assert.Equal((10, StatusPedido.SaiuParaEntrega, StatusPedido.Entregue), _automacoes.Avisos.Single());
        // Ao entregar, o ponto do entregador vai para o endereço do cliente.
        Assert.Equal(-23.36, pedido.EntregadorLatitude);
    }

    [Fact]
    public async Task Pedido_sem_receita_e_entregue_sem_precisar_conferir()
    {
        var pedido = PedidoNaRua(requerReceita: false);

        await _servico.ConfirmarEntregaAsync(10, Entregador, new EntregarDto(ReceitaConferida: false));

        Assert.Equal(StatusPedido.Entregue, pedido.Status);
        Assert.False(pedido.ReceitaConferida);
    }

    [Fact]
    public async Task Marcar_receita_em_pedido_sem_receita_nao_grava_receita_conferida()
    {
        var pedido = PedidoNaRua(requerReceita: false);

        await _servico.ConfirmarEntregaAsync(10, Entregador, new EntregarDto(ReceitaConferida: true));

        Assert.False(pedido.ReceitaConferida);
    }

    [Fact]
    public async Task Entregador_nao_confirma_pedido_de_outro_entregador()
    {
        var pedido = PedidoNaRua(requerReceita: false, idEntregador: 99);

        await Assert.ThrowsAsync<AcessoNegadoException>(
            () => _servico.ConfirmarEntregaAsync(10, Entregador, new EntregarDto(true)));

        Assert.Equal(StatusPedido.SaiuParaEntrega, pedido.Status);
    }

    [Fact]
    public async Task Pedido_que_nao_saiu_para_entrega_nao_pode_ser_entregue()
    {
        var pedido = PedidoNaRua(requerReceita: false, status: StatusPedido.EmSeparacao);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.ConfirmarEntregaAsync(10, Entregador, new EntregarDto(false)));

        Assert.Equal(StatusPedido.EmSeparacao, pedido.Status);
        Assert.Equal(0, _uow.VezesQueSalvou);
    }

    [Fact]
    public async Task Pedido_que_nao_existe_avisa_nao_encontrado() =>
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _servico.ConfirmarEntregaAsync(404, Entregador, new EntregarDto(true)));
}
