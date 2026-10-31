using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Área administrativa: somente perfis ADMIN e ATENDENTE.</summary>
[ApiController]
[Authorize(Roles = Perfis.Equipe)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminServico _admin;
    private readonly IPedidoServico _pedidos;
    private readonly IAssinaturaServico _assinaturas;
    private readonly IDeliveryServico _delivery;
    private readonly IOuvidoriaServico _ouvidoria;

    public AdminController(IAdminServico admin, IPedidoServico pedidos, IAssinaturaServico assinaturas, IDeliveryServico delivery,
                           IOuvidoriaServico ouvidoria)
    {
        _admin = admin;
        _pedidos = pedidos;
        _assinaturas = assinaturas;
        _delivery = delivery;
        _ouvidoria = ouvidoria;
    }

    /// <summary>Indicadores do painel (vendas, pedidos em aberto, estoque baixo, validade próxima).</summary>
    [HttpGet("painel")]
    public async Task<ActionResult<PainelDto>> Painel() => Ok(await _admin.PainelAsync());

    /// <summary>Pedidos de todos os clientes. status: AGUARDANDO_PAGAMENTO, PAGO, ..., ou ABERTOS.</summary>
    [HttpGet("pedidos")]
    public async Task<ActionResult<List<PedidoResumoDto>>> Pedidos([FromQuery] string? status) => Ok(await _pedidos.ListarAsync(status));

    /// <summary>Avança o pedido no fluxo (ex.: PAGO → EM_SEPARACAO).</summary>
    [HttpPatch("pedidos/{id:int}/status")]
    public async Task<ActionResult<PedidoDetalheDto>> AlterarStatusPedido(int id, AlterarStatusDto dto) =>
        Ok(await _pedidos.AlterarStatusAsync(id, dto.Status));

    [HttpGet("clientes")]
    public async Task<ActionResult<List<ClienteDto>>> Clientes() => Ok(await _admin.ClientesAsync());

    /// <summary>Alertas de reposição criados automaticamente (trigger de baixa de estoque).</summary>
    [HttpGet("alertas")]
    public async Task<ActionResult<List<AlertaDto>>> Alertas() => Ok(await _admin.AlertasAsync());

    [HttpPost("alertas/{id:int}/resolver")]
    public async Task<IActionResult> ResolverAlerta(int id)
    {
        await _admin.ResolverAlertaAsync(id);
        return NoContent();
    }

    [HttpGet("assinaturas")]
    public async Task<ActionResult<List<AssinaturaDto>>> Assinaturas() => Ok(await _assinaturas.ListarTodasAsync());

    [HttpGet("delivery")]
    public async Task<ActionResult<List<DeliveryDto>>> Delivery() => Ok(await _delivery.ListarTodasAsync());

    /// <summary>
    /// Abre a foto/PDF da receita (RF39). Só a equipe logada; o arquivo não tem endereço público.
    /// "no-store": o navegador não guarda cópia (dado de saúde).
    /// </summary>
    [HttpGet("delivery/{id:int}/receita")]
    public async Task<IActionResult> ReceitaDelivery(int id)
    {
        var arquivo = await _delivery.AbrirReceitaAsync(id);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(arquivo.Conteudo, arquivo.TipoConteudo);
    }

    [HttpPatch("delivery/{id:int}/status")]
    public async Task<ActionResult<DeliveryDto>> AlterarStatusDelivery(int id, AlterarStatusDto dto) =>
        Ok(await _delivery.AlterarStatusAsync(id, dto.Status));

    /// <summary>Ouvidoria (RF30): em aberto primeiro e, entre elas, denúncias de discriminação primeiro.</summary>
    [HttpGet("ouvidoria")]
    public async Task<ActionResult<List<OuvidoriaDto>>> Ouvidoria() => Ok(await _ouvidoria.ListarAsync());

    /// <summary>Responde e/ou muda a situação (ABERTA, EM_ANALISE, RESPONDIDA, ENCERRADA). Responder já marca RESPONDIDA.</summary>
    [HttpPatch("ouvidoria/{id:int}")]
    public async Task<ActionResult<OuvidoriaDto>> ResponderOuvidoria(int id, OuvidoriaAtualizarDto dto) =>
        Ok(await _ouvidoria.AtualizarAsync(id, dto));
}
