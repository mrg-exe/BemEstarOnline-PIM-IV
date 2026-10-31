using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Gestão da equipe (RF35): somente o perfil ADMIN.</summary>
[ApiController]
[Authorize(Roles = Perfis.Admin)]
[Route("api/admin/equipe")]
public class EquipeController : ControllerBase
{
    private readonly IEquipeServico _equipe;
    public EquipeController(IEquipeServico equipe) => _equipe = equipe;

    /// <summary>Atendentes, entregadores e administradores (ativos primeiro).</summary>
    [HttpGet]
    public async Task<ActionResult<List<EquipeDto>>> Listar() => Ok(await _equipe.ListarAsync());

    /// <summary>Cadastra uma pessoa da equipe com senha inicial provisória (troca obrigatória no primeiro acesso).</summary>
    [HttpPost]
    public async Task<ActionResult<EquipeDto>> Criar(EquipeCriarDto dto) =>
        StatusCode(StatusCodes.Status201Created, await _equipe.CriarAsync(dto));

    /// <summary>Muda o perfil (ATENDENTE, ENTREGADOR, ADMIN) e/ou ativa/desativa a conta.</summary>
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<EquipeDto>> Atualizar(int id, EquipeAtualizarDto dto) =>
        Ok(await _equipe.AtualizarAsync(User.ObterIdUsuario(), id, dto));

    /// <summary>Define uma nova senha provisória (a pessoa troca no próximo acesso).</summary>
    [HttpPost("{id:int}/redefinir-senha")]
    public async Task<ActionResult<EquipeDto>> RedefinirSenha(int id, RedefinirSenhaDto dto) =>
        Ok(await _equipe.RedefinirSenhaAsync(User.ObterIdUsuario(), id, dto.NovaSenha));
}
