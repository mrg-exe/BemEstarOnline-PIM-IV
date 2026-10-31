using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>
/// Carrinho salvo no servidor. O site e o app guardam o carrinho localmente e,
/// quando o cliente está logado, enviam a lista completa para cá (sincronização).
/// </summary>
public class CarrinhoServico : ICarrinhoServico
{
    private readonly ICarrinhoRepositorio _carrinhos;
    private readonly IProdutoRepositorio _produtos;
    private readonly IUnidadeDeTrabalho _uow;

    public CarrinhoServico(ICarrinhoRepositorio carrinhos, IProdutoRepositorio produtos, IUnidadeDeTrabalho uow)
    {
        _carrinhos = carrinhos;
        _produtos = produtos;
        _uow = uow;
    }

    public async Task<CarrinhoDto> ObterAsync(int idUsuario)
    {
        var carrinho = await _carrinhos.ObterPorUsuarioAsync(idUsuario);
        return ParaDto(carrinho);
    }

    public async Task<CarrinhoDto> SubstituirAsync(int idUsuario, List<ItemQuantidadeDto> itens)
    {
        var carrinho = await _carrinhos.ObterPorUsuarioAsync(idUsuario);
        if (carrinho is null)
        {
            carrinho = new Carrinho { IdUsuario = idUsuario };
            _carrinhos.Adicionar(carrinho);
        }

        _carrinhos.RemoverItens(carrinho.Itens.ToList());
        carrinho.Itens.Clear();

        var agrupados = itens.Where(i => i.Quantidade > 0)
                             .GroupBy(i => i.IdMedicamento)
                             .ToDictionary(g => g.Key, g => Math.Min(99, g.Sum(i => i.Quantidade)));
        var produtos = await _produtos.ObterVariosAsync(agrupados.Keys);

        foreach (var produto in produtos.Where(p => p.Ativo))
        {
            carrinho.Itens.Add(new CarrinhoItem
            {
                IdMedicamento = produto.IdMedicamento,
                Medicamento = produto,
                Quantidade = agrupados[produto.IdMedicamento],
                PrecoUnitario = produto.Preco
            });
        }
        carrinho.AtualizadoEm = DateTime.Now;
        await _uow.SalvarAsync();

        return ParaDto(carrinho);
    }

    public async Task LimparAsync(int idUsuario)
    {
        var carrinho = await _carrinhos.ObterPorUsuarioAsync(idUsuario);
        if (carrinho is null || carrinho.Itens.Count == 0) return;
        _carrinhos.RemoverItens(carrinho.Itens.ToList());
        carrinho.Itens.Clear();
        await _uow.SalvarAsync();
    }

    private static CarrinhoDto ParaDto(Carrinho? carrinho)
    {
        if (carrinho is null) return new CarrinhoDto(new List<CarrinhoItemDto>(), 0, 0);
        var itens = carrinho.Itens
            .Where(i => i.Medicamento is not null)
            .Select(i => new CarrinhoItemDto(i.IdMedicamento, i.Medicamento!.Nome, i.Medicamento.Slug, i.Medicamento.Preco,
                i.Quantidade, i.Medicamento.Preco * i.Quantidade, i.Medicamento.ExigeReceita, i.Medicamento.QuantidadeEmEstoque))
            .ToList();
        return new CarrinhoDto(itens, itens.Sum(i => i.Subtotal), itens.Sum(i => i.Quantidade));
    }
}
