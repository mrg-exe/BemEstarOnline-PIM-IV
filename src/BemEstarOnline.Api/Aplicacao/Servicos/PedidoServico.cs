using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>Cálculo de valores, criação, acompanhamento e mudança de status dos pedidos.</summary>
public class PedidoServico : IPedidoServico
{
    private readonly IPedidoRepositorio _pedidos;
    private readonly IProdutoRepositorio _produtos;
    private readonly ICupomRepositorio _cupons;
    private readonly IEnderecoRepositorio _enderecos;
    private readonly ICarrinhoServico _carrinho;
    private readonly IAutomacoesBanco _automacoes;
    private readonly IUnidadeDeTrabalho _uow;

    public PedidoServico(IPedidoRepositorio pedidos, IProdutoRepositorio produtos, ICupomRepositorio cupons,
                         IEnderecoRepositorio enderecos, ICarrinhoServico carrinho, IAutomacoesBanco automacoes,
                         IUnidadeDeTrabalho uow)
    {
        _pedidos = pedidos;
        _produtos = produtos;
        _cupons = cupons;
        _enderecos = enderecos;
        _carrinho = carrinho;
        _automacoes = automacoes;
        _uow = uow;
    }

    // ------------------------------------------------------------------
    // Cálculo (usado pelo carrinho do site/app antes de finalizar)
    // ------------------------------------------------------------------
    public async Task<ResumoValoresDto> SimularAsync(SimulacaoPedidoDto dto)
    {
        var calculo = await CalcularAsync(dto.Itens, dto.CodigoCupom, exigirEstoque: false);
        return calculo.ParaResumo();
    }

    public async Task<CupomResultadoDto> ValidarCupomAsync(CupomValidarDto dto)
    {
        var frete = RegrasComerciais.CalcularFrete(dto.Subtotal);
        var cupom = await _cupons.ObterPorCodigoAsync(dto.Codigo.Trim().ToUpperInvariant());
        if (cupom is null) return new CupomResultadoDto(false, "Cupom não encontrado.", 0, frete, dto.Subtotal + frete);

        var motivo = cupom.MotivoInvalido(dto.Subtotal, DateTime.Now);
        if (motivo is not null) return new CupomResultadoDto(false, motivo, 0, frete, dto.Subtotal + frete);

        var (desconto, freteFinal) = cupom.Aplicar(dto.Subtotal, frete);
        return new CupomResultadoDto(true, cupom.Descricao, desconto, freteFinal, Math.Max(0, dto.Subtotal - desconto + freteFinal));
    }

    // ------------------------------------------------------------------
    // Criação do pedido
    // ------------------------------------------------------------------
    public async Task<PedidoDetalheDto> CriarAsync(int idUsuario, PedidoCriarDto dto)
    {
        if (!MetodoPagamento.Todos.Contains(dto.MetodoPagamento))
            throw new RegraDeNegocioException("Forma de pagamento inválida.");

        var calculo = await CalcularAsync(dto.Itens, dto.CodigoCupom, exigirEstoque: true);
        if (!string.IsNullOrWhiteSpace(dto.CodigoCupom) && calculo.Cupom is null)
            throw new RegraDeNegocioException(calculo.MensagemCupom ?? "Cupom inválido.");

        var pedido = new Pedido
        {
            IdUsuario = idUsuario,
            Canal = dto.Canal == CanalVenda.App ? CanalVenda.App : CanalVenda.Site,
            Subtotal = calculo.Subtotal,
            Desconto = calculo.Desconto,
            Frete = calculo.Frete,
            Total = calculo.Total,
            RequerReceita = calculo.RequerReceita,
            Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim(),
            CriadoEm = DateTime.Now
        };

        // Endereço: um já salvo ou um novo digitado no checkout.
        if (dto.IdEndereco.HasValue)
        {
            var endereco = await _enderecos.ObterPorIdAsync(dto.IdEndereco.Value);
            if (endereco is null || endereco.IdUsuario != idUsuario)
                throw new RegraDeNegocioException("Endereço de entrega inválido.");
            pedido.IdEndereco = endereco.IdEndereco;
            pedido.Endereco = endereco;
        }
        else if (dto.NovoEndereco is not null)
        {
            var existentes = await _enderecos.ListarPorUsuarioAsync(idUsuario);
            var novo = EnderecoServico.NovoEndereco(idUsuario, dto.NovoEndereco, existentes.Count == 0);
            _enderecos.Adicionar(novo);
            pedido.Endereco = novo;
        }
        else
        {
            throw new RegraDeNegocioException("Informe o endereço de entrega.");
        }

        foreach (var item in calculo.Itens)
        {
            pedido.Itens.Add(new PedidoItem
            {
                IdMedicamento = item.Produto.IdMedicamento,
                NomeProduto = item.Produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.Produto.Preco,
                Subtotal = item.Produto.Preco * item.Quantidade
            });
        }

        // Pagamento simulado: PIX e cartão são aprovados na hora; dinheiro é pago na entrega.
        var pagoNaEntrega = dto.MetodoPagamento == MetodoPagamento.Dinheiro;
        pedido.Status = pagoNaEntrega ? StatusPedido.AguardandoPagamento : StatusPedido.Pago;
        pedido.Pagamentos.Add(new Pagamento
        {
            Metodo = dto.MetodoPagamento,
            Status = pagoNaEntrega ? StatusPagamento.Pendente : StatusPagamento.Aprovado,
            Valor = pedido.Total,
            PagoEm = pagoNaEntrega ? null : DateTime.Now,
            CodigoTransacao = $"BEO-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
        });

        if (calculo.Cupom is not null)
        {
            pedido.IdCupom = calculo.Cupom.IdCupom;
            calculo.Cupom.UsosRealizados++;
        }

        _pedidos.Adicionar(pedido);
        await _uow.SalvarAsync();

        // Baixa de estoque + histórico: triggers no MySQL ou regra em C# no modo memória.
        await _automacoes.AposCriarPedidoAsync(pedido);
        await _carrinho.LimparAsync(idUsuario);

        return (await _pedidos.ObterDetalheAsync(pedido.IdPedido))!.ParaDetalhe();
    }

    // ------------------------------------------------------------------
    // Consultas
    // ------------------------------------------------------------------
    public async Task<List<PedidoResumoDto>> ListarDoUsuarioAsync(int idUsuario) =>
        (await _pedidos.ListarPorUsuarioAsync(idUsuario)).Select(p => p.ParaResumo()).ToList();

    public async Task<List<PedidoResumoDto>> ListarAsync(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status) && status != "ABERTOS" && !StatusPedido.Todos.Contains(status))
            throw new RegraDeNegocioException("Filtro de status inválido.");
        return (await _pedidos.ListarAsync(status, 300)).Select(p => p.ParaResumo()).ToList();
    }

    public async Task<PedidoDetalheDto> ObterAsync(int idPedido, int idUsuarioSolicitante, bool solicitanteEhEquipe)
    {
        var pedido = await _pedidos.ObterDetalheAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (!solicitanteEhEquipe && pedido.IdUsuario != idUsuarioSolicitante) throw new AcessoNegadoException();
        return pedido.ParaDetalhe();
    }

    public async Task<RastreioDto> RastreioAsync(int idPedido, int idUsuarioSolicitante, bool solicitanteEhEquipe)
    {
        var pedido = await _pedidos.ObterDetalheAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (!solicitanteEhEquipe && pedido.IdUsuario != idUsuarioSolicitante) throw new AcessoNegadoException();
        return pedido.ParaRastreio();
    }

    // ------------------------------------------------------------------
    // Mudanças de status
    // ------------------------------------------------------------------
    public async Task<PedidoDetalheDto> CancelarAsync(int idPedido, int idUsuario)
    {
        var pedido = await _pedidos.ObterDetalheAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (pedido.IdUsuario != idUsuario) throw new AcessoNegadoException();
        if (!pedido.ClientePodeCancelar)
            throw new RegraDeNegocioException("Este pedido já está sendo preparado e não pode mais ser cancelado pelo site. Fale com a farmácia.");
        return await MudarStatusAsync(pedido, StatusPedido.Cancelado);
    }

    public async Task<PedidoDetalheDto> AlterarStatusAsync(int idPedido, string novoStatus)
    {
        var pedido = await _pedidos.ObterDetalheAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        return await MudarStatusAsync(pedido, novoStatus);
    }

    private async Task<PedidoDetalheDto> MudarStatusAsync(Pedido pedido, string novoStatus)
    {
        var anterior = pedido.AlterarStatus(novoStatus);
        await _uow.SalvarAsync();
        await _automacoes.AposAlterarStatusAsync(pedido, anterior);
        return (await _pedidos.ObterDetalheAsync(pedido.IdPedido))!.ParaDetalhe();
    }

    // ------------------------------------------------------------------
    // Cálculo de valores (fonte única para simulação e criação)
    // ------------------------------------------------------------------
    private record ItemCalculado(Medicamento Produto, int Quantidade);

    private record Calculo(List<ItemCalculado> Itens, decimal Subtotal, decimal Desconto, decimal Frete, decimal Total,
                           Cupom? Cupom, string? MensagemCupom, bool RequerReceita)
    {
        public ResumoValoresDto ParaResumo() => new(
            Itens.Select(i => new CarrinhoItemDto(i.Produto.IdMedicamento, i.Produto.Nome, i.Produto.Slug, i.Produto.Preco,
                i.Quantidade, i.Produto.Preco * i.Quantidade, i.Produto.ExigeReceita, i.Produto.QuantidadeEmEstoque)).ToList(),
            Subtotal, Desconto, Frete, Total, Cupom?.Codigo, MensagemCupom, RequerReceita,
            Math.Max(0, RegrasComerciais.FreteGratisAPartirDe - Subtotal));
    }

    private async Task<Calculo> CalcularAsync(List<ItemQuantidadeDto> itensPedido, string? codigoCupom, bool exigirEstoque)
    {
        if (itensPedido is null || itensPedido.Count == 0) throw new RegraDeNegocioException("O carrinho está vazio.");

        var agrupados = itensPedido.Where(i => i.Quantidade > 0)
                                   .GroupBy(i => i.IdMedicamento)
                                   .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantidade));
        if (agrupados.Count == 0) throw new RegraDeNegocioException("O carrinho está vazio.");

        var produtos = await _produtos.ObterVariosAsync(agrupados.Keys);
        var itens = new List<ItemCalculado>();
        foreach (var (idProduto, quantidade) in agrupados)
        {
            var produto = produtos.FirstOrDefault(p => p.IdMedicamento == idProduto);
            if (produto is null || !produto.Ativo)
                throw new RegraDeNegocioException("Um dos produtos do carrinho não está mais à venda. Remova-o e tente de novo.");
            if (exigirEstoque && produto.QuantidadeEmEstoque < quantidade)
                throw new RegraDeNegocioException(produto.QuantidadeEmEstoque == 0
                    ? $"{produto.Nome} acabou de esgotar."
                    : $"Temos só {produto.QuantidadeEmEstoque} unidade(s) de {produto.Nome}.");
            itens.Add(new ItemCalculado(produto, quantidade));
        }

        var subtotal = itens.Sum(i => i.Produto.Preco * i.Quantidade);
        var frete = RegrasComerciais.CalcularFrete(subtotal);
        decimal desconto = 0;
        Cupom? cupomAplicado = null;
        string? mensagem = null;

        if (!string.IsNullOrWhiteSpace(codigoCupom))
        {
            var cupom = await _cupons.ObterPorCodigoAsync(codigoCupom.Trim().ToUpperInvariant());
            var motivo = cupom is null ? "Cupom não encontrado." : cupom.MotivoInvalido(subtotal, DateTime.Now);
            if (motivo is null)
            {
                (desconto, frete) = cupom!.Aplicar(subtotal, frete);
                cupomAplicado = cupom;
                mensagem = cupom.Descricao;
            }
            else mensagem = motivo;
        }

        var total = Math.Max(0, subtotal - desconto + frete);
        return new Calculo(itens, subtotal, desconto, frete, total, cupomAplicado, mensagem,
                           itens.Any(i => i.Produto.ExigeReceita));
    }
}
