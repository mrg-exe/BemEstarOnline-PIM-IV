using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Carrinho salvo do cliente logado (sincroniza site e app).</summary>
[ApiController]
[Authorize]
[Route("api/carrinho")]
public class CarrinhoController : ControllerBase
{
    private readonly ICarrinhoServico _carrinho;
    public CarrinhoController(ICarrinhoServico carrinho) => _carrinho = carrinho;

    [HttpGet]
    public async Task<ActionResult<CarrinhoDto>> Obter() => Ok(await _carrinho.ObterAsync(User.ObterIdUsuario()));

    /// <summary>Substitui o carrinho inteiro pela lista enviada.</summary>
    [HttpPut]
    public async Task<ActionResult<CarrinhoDto>> Substituir(List<ItemQuantidadeDto> itens) =>
        Ok(await _carrinho.SubstituirAsync(User.ObterIdUsuario(), itens));

    [HttpDelete]
    public async Task<IActionResult> Limpar()
    {
        await _carrinho.LimparAsync(User.ObterIdUsuario());
        return NoContent();
    }
}

/// <summary>Endereços de entrega do cliente logado.</summary>
[ApiController]
[Authorize]
[Route("api/enderecos")]
public class EnderecosController : ControllerBase
{
    private readonly IEnderecoServico _enderecos;
    public EnderecosController(IEnderecoServico enderecos) => _enderecos = enderecos;

    [HttpGet]
    public async Task<ActionResult<List<EnderecoDto>>> Listar() => Ok(await _enderecos.ListarAsync(User.ObterIdUsuario()));

    [HttpPost]
    public async Task<ActionResult<EnderecoDto>> Criar(EnderecoSalvarDto dto) =>
        StatusCode(StatusCodes.Status201Created, await _enderecos.CriarAsync(User.ObterIdUsuario(), dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        await _enderecos.RemoverAsync(User.ObterIdUsuario(), id);
        return NoContent();
    }
}

/// <summary>Assinaturas de medicamentos de uso contínuo.</summary>
[ApiController]
[Authorize]
[Route("api/assinaturas")]
public class AssinaturasController : ControllerBase
{
    private readonly IAssinaturaServico _assinaturas;
    public AssinaturasController(IAssinaturaServico assinaturas) => _assinaturas = assinaturas;

    [HttpGet]
    public async Task<ActionResult<List<AssinaturaDto>>> Minhas() => Ok(await _assinaturas.ListarDoUsuarioAsync(User.ObterIdUsuario()));

    [HttpPost]
    public async Task<ActionResult<AssinaturaDto>> Criar(AssinaturaCriarDto dto) =>
        StatusCode(StatusCodes.Status201Created, await _assinaturas.CriarAsync(User.ObterIdUsuario(), dto));

    /// <summary>Pausa, reativa ou cancela (status: ATIVA, PAUSADA ou CANCELADA).</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AssinaturaDto>> AlterarStatus(int id, AlterarStatusDto dto) =>
        Ok(await _assinaturas.AlterarStatusAsync(id, dto.Status, User.ObterIdUsuario(), User.EhEquipe()));
}

/// <summary>Delivery expresso por texto/receita (não exige login).</summary>
[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryServico _delivery;
    public DeliveryController(IDeliveryServico delivery) => _delivery = delivery;

    /// <summary>
    /// Envio como formulário com arquivo (multipart/form-data): os campos do pedido e, se tiver, o arquivo
    /// "receita" (JPG, PNG ou PDF até 5 MB), que vai para a pasta privada da API (RF39).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<DeliveryDto>> Criar([FromForm] DeliveryCriarDto dto, IFormFile? receita)
    {
        int? idUsuario = User.Identity?.IsAuthenticated == true ? User.ObterIdUsuario() : null;
        var arquivo = receita is null ? null : new ArquivoRecebido(receita.FileName, receita.Length, receita.OpenReadStream);
        return StatusCode(StatusCodes.Status201Created, await _delivery.CriarAsync(idUsuario, dto, arquivo));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<DeliveryDto>>> Minhas() => Ok(await _delivery.ListarDoUsuarioAsync(User.ObterIdUsuario()));
}
