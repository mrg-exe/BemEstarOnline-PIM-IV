using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>
/// Ouvidoria e canal de denúncia de discriminação (RF30). Não exige login.
/// As rotas da equipe (listar e responder) ficam no AdminController.
/// </summary>
[ApiController]
[Route("api/ouvidoria")]
public class OuvidoriaController : ControllerBase
{
    private readonly IOuvidoriaServico _ouvidoria;
    public OuvidoriaController(IOuvidoriaServico ouvidoria) => _ouvidoria = ouvidoria;

    /// <summary>
    /// Registra a manifestação e devolve o protocolo. Tipos: DISCRIMINACAO (com subtipoDiscriminacao:
    /// RACIAL, RELIGIOSA, GENERO, DEFICIENCIA, IDADE ou OUTRA), ATENDIMENTO, ENTREGA, SUGESTAO, ELOGIO.
    /// Se estiver logado e não for anônimo, a conta fica ligada ao relato. Anônimo não guarda conta nem contato.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OuvidoriaProtocoloDto>> Registrar(OuvidoriaCriarDto dto)
    {
        int? idUsuario = User.Identity?.IsAuthenticated == true ? User.ObterIdUsuario() : null;
        return StatusCode(StatusCodes.Status201Created, await _ouvidoria.RegistrarAsync(idUsuario, dto));
    }

    /// <summary>Consulta a situação e a resposta pelo protocolo (ex.: OUV-2026-7K3M9QXA).</summary>
    [HttpGet("{protocolo}")]
    public async Task<ActionResult<OuvidoriaProtocoloDto>> Consultar(string protocolo) =>
        Ok(await _ouvidoria.ConsultarAsync(protocolo));
}
