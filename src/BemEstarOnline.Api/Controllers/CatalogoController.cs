using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Catálogo público de produtos e manutenção pela equipe.</summary>
[ApiController]
[Route("api")]
public class CatalogoController : ControllerBase
{
    private readonly ICatalogoServico _catalogo;
    public CatalogoController(ICatalogoServico catalogo) => _catalogo = catalogo;

    /// <summary>Lista as categorias com a quantidade de produtos de cada uma.</summary>
    [HttpGet("categorias")]
    public async Task<ActionResult<List<CategoriaDto>>> Categorias() => Ok(await _catalogo.ListarCategoriasAsync());

    /// <summary>Lista os produtos. Filtros opcionais: categoria (slug), busca (texto), farmaciaPopular, usoContinuo.</summary>
    [HttpGet("produtos")]
    public async Task<ActionResult<List<ProdutoDto>>> Produtos([FromQuery] string? categoria, [FromQuery] string? busca,
        [FromQuery] bool? farmaciaPopular, [FromQuery] bool? usoContinuo) =>
        Ok(await _catalogo.ListarProdutosAsync(new FiltroProdutos(categoria, busca, farmaciaPopular, usoContinuo)));

    /// <summary>Detalhe de um produto pelo número (id) ou pelo nome na URL (slug).</summary>
    [HttpGet("produtos/{idOuSlug}")]
    public async Task<ActionResult<ProdutoDto>> Produto(string idOuSlug) => Ok(await _catalogo.ObterProdutoAsync(idOuSlug));

    /// <summary>[Equipe] Lista todos os produtos, inclusive os desativados.</summary>
    [Authorize(Roles = Perfis.Equipe)]
    [HttpGet("admin/produtos")]
    public async Task<ActionResult<List<ProdutoDto>>> TodosProdutos([FromQuery] string? busca) =>
        Ok(await _catalogo.ListarProdutosAsync(new FiltroProdutos(Busca: busca, IncluirInativos: true)));

    /// <summary>[Admin] Cadastra um produto.</summary>
    [Authorize(Roles = Perfis.Admin)]
    [HttpPost("admin/produtos")]
    public async Task<ActionResult<ProdutoDto>> Criar(ProdutoSalvarDto dto) =>
        StatusCode(StatusCodes.Status201Created, await _catalogo.CriarProdutoAsync(dto));

    /// <summary>[Admin] Atualiza os dados de um produto.</summary>
    [Authorize(Roles = Perfis.Admin)]
    [HttpPut("admin/produtos/{id:int}")]
    public async Task<ActionResult<ProdutoDto>> Atualizar(int id, ProdutoSalvarDto dto) =>
        Ok(await _catalogo.AtualizarProdutoAsync(id, dto));

    /// <summary>
    /// [Admin] Envia a foto do produto (JPG, PNG ou WEBP, até 3 MB). Ela fica salva na pasta
    /// wwwroot/imagens/produtos da API e aparece no site e no app.
    /// </summary>
    [Authorize(Roles = Perfis.Admin)]
    [HttpPost("admin/produtos/{id:int}/imagem")]
    [RequestSizeLimit(3_200_000)]
    public async Task<ActionResult<ProdutoDto>> EnviarImagem(int id, IFormFile arquivo, [FromServices] IWebHostEnvironment ambiente)
    {
        var extensoes = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (arquivo.Length == 0 || arquivo.Length > 3_000_000 || !extensoes.Contains(extensao))
            return BadRequest(new { mensagem = "Envie uma foto JPG, PNG ou WEBP de até 3 MB." });

        var produto = await _catalogo.ObterProdutoAsync(id.ToString());
        var pasta = Path.Combine(ambiente.ContentRootPath, "wwwroot", "imagens", "produtos");
        Directory.CreateDirectory(pasta);
        foreach (var antigo in Directory.GetFiles(pasta, $"{produto.Slug}.*")) System.IO.File.Delete(antigo);

        var nomeArquivo = $"{produto.Slug}{extensao}";
        await using (var destino = System.IO.File.Create(Path.Combine(pasta, nomeArquivo)))
            await arquivo.CopyToAsync(destino);

        return Ok(await _catalogo.DefinirImagemAsync(id, $"/imagens/produtos/{nomeArquivo}?v={DateTime.Now:yyyyMMddHHmmss}"));
    }

    /// <summary>[Equipe] Ajusta a quantidade em estoque (ex.: chegada de mercadoria).</summary>
    [Authorize(Roles = Perfis.Equipe)]
    [HttpPatch("admin/produtos/{id:int}/estoque")]
    public async Task<ActionResult<ProdutoDto>> AjustarEstoque(int id, EstoqueAjusteDto dto) =>
        Ok(await _catalogo.AjustarEstoqueAsync(id, dto));
}
