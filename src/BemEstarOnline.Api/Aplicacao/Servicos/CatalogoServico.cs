using System.Globalization;
using System.Text;
using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>Consulta do catálogo (público) e manutenção de produtos/estoque (equipe).</summary>
public class CatalogoServico : ICatalogoServico
{
    private readonly ICategoriaRepositorio _categorias;
    private readonly IProdutoRepositorio _produtos;
    private readonly IAlertaEstoqueRepositorio _alertas;
    private readonly IUnidadeDeTrabalho _uow;

    public CatalogoServico(ICategoriaRepositorio categorias, IProdutoRepositorio produtos,
                           IAlertaEstoqueRepositorio alertas, IUnidadeDeTrabalho uow)
    {
        _categorias = categorias;
        _produtos = produtos;
        _alertas = alertas;
        _uow = uow;
    }

    public async Task<List<CategoriaDto>> ListarCategoriasAsync()
    {
        var categorias = await _categorias.ListarAsync();
        return categorias
            .Select(c => new CategoriaDto(c.IdCategoria, c.Nome, c.Slug, c.Descricao, c.Icone,
                                          c.Medicamentos.Count(m => m.Ativo)))
            .ToList();
    }

    public async Task<List<ProdutoDto>> ListarProdutosAsync(FiltroProdutos filtro) =>
        (await _produtos.ListarAsync(filtro)).Select(p => p.ParaDto()).ToList();

    public async Task<ProdutoDto> ObterProdutoAsync(string idOuSlug)
    {
        var produto = int.TryParse(idOuSlug, out var id)
            ? await _produtos.ObterPorIdAsync(id)
            : await _produtos.ObterPorSlugAsync(idOuSlug);
        if (produto is null) throw new NaoEncontradoException("Produto não encontrado.");
        return produto.ParaDto();
    }

    public async Task<ProdutoDto> CriarProdutoAsync(ProdutoSalvarDto dto)
    {
        await GarantirCategoriaAsync(dto.IdCategoria);
        var slug = await GerarSlugUnicoAsync(dto.Nome, null);

        var produto = new Medicamento { Slug = slug };
        Preencher(produto, dto);
        produto.Estoque = new Estoque
        {
            Quantidade = Math.Max(0, dto.EstoqueInicial ?? 0),
            QuantidadeMinima = Math.Max(0, dto.EstoqueMinimo ?? 5),
            Localizacao = "Depósito"
        };
        _produtos.Adicionar(produto);
        await _uow.SalvarAsync();

        return (await _produtos.ObterPorIdAsync(produto.IdMedicamento))!.ParaDto();
    }

    public async Task<ProdutoDto> AtualizarProdutoAsync(int id, ProdutoSalvarDto dto)
    {
        var produto = await _produtos.ObterPorIdAsync(id) ?? throw new NaoEncontradoException("Produto não encontrado.");
        await GarantirCategoriaAsync(dto.IdCategoria);

        if (!string.Equals(produto.Nome, dto.Nome, StringComparison.OrdinalIgnoreCase))
            produto.Slug = await GerarSlugUnicoAsync(dto.Nome, id);

        Preencher(produto, dto);
        produto.AtualizadoEm = DateTime.Now;
        if (dto.EstoqueMinimo.HasValue && produto.Estoque is not null)
            produto.Estoque.QuantidadeMinima = Math.Max(0, dto.EstoqueMinimo.Value);

        await _uow.SalvarAsync();
        return (await _produtos.ObterPorIdAsync(id))!.ParaDto();
    }

    public async Task<ProdutoDto> AjustarEstoqueAsync(int id, EstoqueAjusteDto dto)
    {
        var produto = await _produtos.ObterPorIdAsync(id) ?? throw new NaoEncontradoException("Produto não encontrado.");
        produto.Estoque ??= new Estoque { IdMedicamento = id, Localizacao = "Depósito" };
        produto.Estoque.Quantidade = dto.Quantidade;
        if (dto.QuantidadeMinima.HasValue) produto.Estoque.QuantidadeMinima = dto.QuantidadeMinima.Value;
        produto.Estoque.AtualizadoEm = DateTime.Now;

        // Reposição feita: os alertas pendentes deste produto são encerrados.
        if (!produto.Estoque.AbaixoDoMinimo)
        {
            foreach (var alerta in await _alertas.ListarPendentesDoProdutoAsync(id))
            {
                alerta.Resolvido = true;
                alerta.ResolvidoEm = DateTime.Now;
            }
        }

        await _uow.SalvarAsync();
        return (await _produtos.ObterPorIdAsync(id))!.ParaDto();
    }

    private static void Preencher(Medicamento p, ProdutoSalvarDto dto)
    {
        p.Nome = dto.Nome.Trim();
        p.IdCategoria = dto.IdCategoria;
        p.Tipo = dto.Tipo.Trim();
        p.PrincipioAtivo = Vazio(dto.PrincipioAtivo);
        p.Descricao = dto.Descricao.Trim();
        p.Dosagem = Vazio(dto.Dosagem);
        p.Apresentacao = Vazio(dto.Apresentacao);
        p.Fabricante = dto.Fabricante.Trim();
        p.ViaAdministracao = Vazio(dto.ViaAdministracao);
        p.RegistroAnvisa = Vazio(dto.RegistroAnvisa);
        p.ValidadeLote = dto.ValidadeLote?.Date;
        p.Preco = Math.Round(dto.Preco, 2);
        p.ExigeReceita = dto.ExigeReceita;
        p.UsoContinuo = dto.UsoContinuo;
        p.FarmaciaPopular = dto.FarmaciaPopular;
        p.Ativo = dto.Ativo;
        if (dto.ImagemUrl is not null) p.ImagemUrl = Vazio(dto.ImagemUrl);
    }

    public async Task<ProdutoDto> DefinirImagemAsync(int id, string? imagemUrl)
    {
        var produto = await _produtos.ObterPorIdAsync(id) ?? throw new NaoEncontradoException("Produto não encontrado.");
        produto.ImagemUrl = Vazio(imagemUrl);
        produto.AtualizadoEm = DateTime.Now;
        await _uow.SalvarAsync();
        return produto.ParaDto();
    }

    private static string? Vazio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task GarantirCategoriaAsync(int idCategoria)
    {
        if (await _categorias.ObterPorIdAsync(idCategoria) is null)
            throw new RegraDeNegocioException("Categoria inválida.");
    }

    private async Task<string> GerarSlugUnicoAsync(string nome, int? ignorarId)
    {
        var baseSlug = CriarSlug(nome);
        var slug = baseSlug;
        var contador = 2;
        while (await _produtos.SlugExisteAsync(slug, ignorarId)) slug = $"{baseSlug}-{contador++}";
        return slug;
    }

    /// <summary>"Losartana Potássica 50mg" vira "losartana-potassica-50mg" (usado na URL do produto).</summary>
    public static string CriarSlug(string texto)
    {
        var semAcento = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) semAcento.Append(c);

        var resultado = new StringBuilder();
        foreach (var c in semAcento.ToString().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) resultado.Append(c);
            else if (resultado.Length > 0 && resultado[^1] != '-') resultado.Append('-');
        }
        return resultado.ToString().Trim('-');
    }
}
