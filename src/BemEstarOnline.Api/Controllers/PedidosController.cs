using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Cálculo de valores, cupons e pedidos do cliente.</summary>
[ApiController]
[Route("api")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoServico _pedidos;
    public PedidosController(IPedidoServico pedidos) => _pedidos = pedidos;

    /// <summary>Calcula subtotal, desconto, frete e total sem gravar nada (usado no carrinho).</summary>
    [HttpPost("pedidos/simular")]
    public async Task<ActionResult<ResumoValoresDto>> Simular(SimulacaoPedidoDto dto) => Ok(await _pedidos.SimularAsync(dto));

    /// <summary>Confere se um cupom vale para o valor informado.</summary>
    [HttpPost("cupons/validar")]
    public async Task<ActionResult<CupomResultadoDto>> ValidarCupom(CupomValidarDto dto) => Ok(await _pedidos.ValidarCupomAsync(dto));

    /// <summary>Finaliza a compra. Os preços são recalculados aqui no servidor (o valor enviado pelo site não é confiável).</summary>
    [Authorize]
    [HttpPost("pedidos")]
    public async Task<ActionResult<PedidoDetalheDto>> Criar(PedidoCriarDto dto)
    {
        var pedido = await _pedidos.CriarAsync(User.ObterIdUsuario(), dto);
        return CreatedAtAction(nameof(Obter), new { id = pedido.IdPedido }, pedido);
    }

    /// <summary>Pedidos do cliente logado.</summary>
    [Authorize]
    [HttpGet("pedidos")]
    public async Task<ActionResult<List<PedidoResumoDto>>> Meus() => Ok(await _pedidos.ListarDoUsuarioAsync(User.ObterIdUsuario()));

    /// <summary>Detalhe e linha do tempo (rastreamento). O cliente só vê os próprios pedidos.</summary>
    [Authorize]
    [HttpGet("pedidos/{id:int}")]
    public async Task<ActionResult<PedidoDetalheDto>> Obter(int id) =>
        Ok(await _pedidos.ObterAsync(id, User.ObterIdUsuario(), User.EhEquipe()));

    /// <summary>Posição do entregador para o mapa (o site e o app consultam a cada poucos segundos).</summary>
    [Authorize]
    [HttpGet("pedidos/{id:int}/rastreio")]
    public async Task<ActionResult<RastreioDto>> Rastreio(int id) =>
        Ok(await _pedidos.RastreioAsync(id, User.ObterIdUsuario(), User.EhEquipe()));

    /// <summary>O cliente cancela o próprio pedido (antes de entrar em separação).</summary>
    [Authorize]
    [HttpPost("pedidos/{id:int}/cancelar")]
    public async Task<ActionResult<PedidoDetalheDto>> Cancelar(int id) =>
        Ok(await _pedidos.CancelarAsync(id, User.ObterIdUsuario()));
}
