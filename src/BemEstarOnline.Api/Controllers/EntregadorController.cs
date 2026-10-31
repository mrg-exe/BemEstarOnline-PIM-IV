using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Rotas usadas pelo app no modo entregador.</summary>
[ApiController]
[Authorize(Roles = Perfis.Entregador + "," + Perfis.Admin)]
[Route("api/entregador")]
public class EntregadorController : ControllerBase
{
    private readonly IEntregaServico _entregas;
    public EntregadorController(IEntregaServico entregas) => _entregas = entregas;

    /// <summary>Pedidos disponíveis (em separação, sem entregador) e as minhas entregas em andamento.</summary>
    [HttpGet("pedidos")]
    public async Task<ActionResult<List<EntregaDto>>> Listar() => Ok(await _entregas.ListarAsync(User.ObterIdUsuario()));

    /// <summary>Pegar um pedido para entregar.</summary>
    [HttpPost("pedidos/{id:int}/aceitar")]
    public async Task<ActionResult<EntregaDto>> Aceitar(int id) => Ok(await _entregas.AceitarAsync(id, User.ObterIdUsuario()));

    /// <summary>Saiu da loja: o pedido passa para SAIU_PARA_ENTREGA e o cliente começa a ver o mapa.</summary>
    [HttpPost("pedidos/{id:int}/sair")]
    public async Task<ActionResult<EntregaDto>> Sair(int id) => Ok(await _entregas.SairParaEntregaAsync(id, User.ObterIdUsuario()));

    /// <summary>Posição atual do entregador (o app manda a cada poucos segundos).</summary>
    [HttpPut("pedidos/{id:int}/localizacao")]
    public async Task<IActionResult> Localizacao(int id, LocalizacaoDto dto)
    {
        await _entregas.AtualizarLocalizacaoAsync(id, User.ObterIdUsuario(), dto);
        return NoContent();
    }

    /// <summary>Confirma a entrega. Se o pedido exige receita, receitaConferida precisa ser true.</summary>
    [HttpPost("pedidos/{id:int}/entregar")]
    public async Task<ActionResult<EntregaDto>> Entregar(int id, EntregarDto dto) =>
        Ok(await _entregas.ConfirmarEntregaAsync(id, User.ObterIdUsuario(), dto));
}
