using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>
/// Fluxo do entregador no app: vê pedidos em separação, pega um, sai para entrega
/// (o app manda a posição GPS de tempos em tempos) e confirma a entrega,
/// conferindo a receita quando o pedido tem remédio de tarja vermelha.
/// </summary>
public class EntregaServico : IEntregaServico
{
    private readonly IPedidoRepositorio _pedidos;
    private readonly IAutomacoesBanco _automacoes;
    private readonly IUnidadeDeTrabalho _uow;

    public EntregaServico(IPedidoRepositorio pedidos, IAutomacoesBanco automacoes, IUnidadeDeTrabalho uow)
    {
        _pedidos = pedidos;
        _automacoes = automacoes;
        _uow = uow;
    }

    public async Task<List<EntregaDto>> ListarAsync(int idEntregador) =>
        (await _pedidos.ListarParaEntregadorAsync(idEntregador)).Select(p => p.ParaEntrega(idEntregador)).ToList();

    public async Task<EntregaDto> AceitarAsync(int idPedido, int idEntregador)
    {
        var pedido = await ObterAsync(idPedido);
        pedido.AtribuirEntregador(idEntregador);
        await _uow.SalvarAsync();
        return pedido.ParaEntrega(idEntregador);
    }

    public async Task<EntregaDto> SairParaEntregaAsync(int idPedido, int idEntregador)
    {
        var pedido = await ObterDoEntregadorAsync(idPedido, idEntregador);
        var anterior = pedido.AlterarStatus(StatusPedido.SaiuParaEntrega);
        // Começa no endereço da loja; o app atualiza a posição durante o trajeto.
        pedido.AtualizarLocalizacao(RegrasComerciais.LojaLatitude, RegrasComerciais.LojaLongitude);
        await _uow.SalvarAsync();
        await _automacoes.AposAlterarStatusAsync(pedido, anterior);
        return pedido.ParaEntrega(idEntregador);
    }

    public async Task AtualizarLocalizacaoAsync(int idPedido, int idEntregador, LocalizacaoDto dto)
    {
        var pedido = await ObterDoEntregadorAsync(idPedido, idEntregador);
        if (pedido.Status != StatusPedido.SaiuParaEntrega)
            throw new RegraDeNegocioException("A localização só é enviada durante a entrega.");
        pedido.AtualizarLocalizacao(dto.Latitude, dto.Longitude);
        await _uow.SalvarAsync();
    }

    public async Task<EntregaDto> ConfirmarEntregaAsync(int idPedido, int idEntregador, EntregarDto dto)
    {
        var pedido = await ObterDoEntregadorAsync(idPedido, idEntregador);
        if (pedido.RequerReceita && !dto.ReceitaConferida)
            throw new RegraDeNegocioException("Este pedido tem remédio com receita. Confira a receita do cliente antes de entregar.");

        pedido.ReceitaConferida = pedido.RequerReceita && dto.ReceitaConferida;
        var anterior = pedido.AlterarStatus(StatusPedido.Entregue);
        if (pedido.Endereco is not null) pedido.AtualizarLocalizacao(
            pedido.Endereco.Latitude ?? pedido.EntregadorLatitude ?? RegrasComerciais.LojaLatitude,
            pedido.Endereco.Longitude ?? pedido.EntregadorLongitude ?? RegrasComerciais.LojaLongitude);
        await _uow.SalvarAsync();
        await _automacoes.AposAlterarStatusAsync(pedido, anterior);
        return pedido.ParaEntrega(idEntregador);
    }

    private async Task<Pedido> ObterAsync(int idPedido) =>
        await _pedidos.ObterDetalheAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");

    private async Task<Pedido> ObterDoEntregadorAsync(int idPedido, int idEntregador)
    {
        var pedido = await ObterAsync(idPedido);
        if (pedido.IdEntregador != idEntregador) throw new AcessoNegadoException("Este pedido não está com você.");
        return pedido;
    }
}
