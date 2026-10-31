namespace BemEstarOnline.Api.Dominio.Entidades;

/// <summary>Carrinho salvo do usuário (permite começar no site e terminar no app).</summary>
public class Carrinho
{
    public int IdCarrinho { get; set; }
    public int IdUsuario { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? AtualizadoEm { get; set; }

    public Usuario? Usuario { get; set; }
    public ICollection<CarrinhoItem> Itens { get; set; } = new List<CarrinhoItem>();
}

public class CarrinhoItem
{
    public int IdItemCarrinho { get; set; }
    public int IdCarrinho { get; set; }
    public int IdMedicamento { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime AdicionadoEm { get; set; } = DateTime.Now;

    public Carrinho? Carrinho { get; set; }
    public Medicamento? Medicamento { get; set; }
}

/// <summary>Cupom promocional com as regras de desconto.</summary>
public class Cupom
{
    public int IdCupom { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Tipo { get; set; } = TipoCupom.Percentual;
    public decimal Valor { get; set; }
    public decimal ValorMinimoPedido { get; set; }
    public DateTime DataInicio { get; set; } = DateTime.Now;
    public DateTime? DataFim { get; set; }
    public int? LimiteUso { get; set; }
    public int UsosRealizados { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Verifica se o cupom pode ser usado; devolve null quando está tudo certo.</summary>
    public string? MotivoInvalido(decimal subtotal, DateTime agora)
    {
        if (!Ativo) return "Este cupom não está mais ativo.";
        if (agora < DataInicio) return "Este cupom ainda não começou a valer.";
        if (DataFim.HasValue && DataFim.Value < agora) return "Este cupom expirou.";
        if (LimiteUso.HasValue && UsosRealizados >= LimiteUso.Value) return "Este cupom atingiu o limite de usos.";
        if (subtotal < ValorMinimoPedido) return $"Este cupom vale para compras a partir de R$ {ValorMinimoPedido:N2}.";
        return null;
    }

    /// <summary>Calcula o desconto e o frete final aplicando este cupom.</summary>
    public (decimal desconto, decimal frete) Aplicar(decimal subtotal, decimal frete) => Tipo switch
    {
        TipoCupom.Percentual => (Math.Round(subtotal * Valor / 100m, 2), frete),
        TipoCupom.ValorFixo => (Math.Min(Valor, subtotal), frete),
        TipoCupom.FreteGratis => (0m, 0m),
        _ => (0m, frete)
    };
}

/// <summary>Compra realizada por um cliente.</summary>
public class Pedido
{
    public int IdPedido { get; set; }
    public int IdUsuario { get; set; }
    public int IdEndereco { get; set; }
    public int? IdCupom { get; set; }
    public string Status { get; set; } = StatusPedido.AguardandoPagamento;
    public string Canal { get; set; } = CanalVenda.Site;
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public decimal Total { get; set; }
    public bool RequerReceita { get; set; }
    /// <summary>O entregador confirmou que conferiu a receita na entrega.</summary>
    public bool ReceitaConferida { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? AtualizadoEm { get; set; }

    // Entrega
    public int? IdEntregador { get; set; }
    public double? EntregadorLatitude { get; set; }
    public double? EntregadorLongitude { get; set; }
    public DateTime? LocalizacaoEm { get; set; }
    public Usuario? Entregador { get; set; }

    public Usuario? Usuario { get; set; }
    public Endereco? Endereco { get; set; }
    public Cupom? Cupom { get; set; }
    public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
    public ICollection<PedidoHistoricoStatus> Historico { get; set; } = new List<PedidoHistoricoStatus>();

    /// <summary>O cliente só pode cancelar antes de o pedido entrar em separação.</summary>
    public bool ClientePodeCancelar =>
        Status == StatusPedido.AguardandoPagamento || Status == StatusPedido.Pago;

    /// <summary>O entregador assume o pedido (só pedidos em separação e ainda sem entregador).</summary>
    public void AtribuirEntregador(int idEntregador)
    {
        if (Status != StatusPedido.EmSeparacao)
            throw new RegraDeNegocioException("Só dá para pegar pedidos que estão em separação.");
        if (IdEntregador.HasValue && IdEntregador != idEntregador)
            throw new RegraDeNegocioException("Outro entregador já pegou este pedido.");
        IdEntregador = idEntregador;
        AtualizadoEm = DateTime.Now;
    }

    public void AtualizarLocalizacao(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new RegraDeNegocioException("Coordenadas inválidas.");
        EntregadorLatitude = latitude;
        EntregadorLongitude = longitude;
        LocalizacaoEm = DateTime.Now;
    }

    /// <summary>Muda o status respeitando o fluxo permitido. Devolve o status anterior.</summary>
    public string AlterarStatus(string novoStatus)
    {
        if (!StatusPedido.Todos.Contains(novoStatus))
            throw new RegraDeNegocioException($"Status '{novoStatus}' não existe.");
        if (!StatusPedido.PodeMudar(Status, novoStatus))
            throw new RegraDeNegocioException($"Não é possível mudar o pedido de {Status} para {novoStatus}.");

        var anterior = Status;
        Status = novoStatus;
        AtualizadoEm = DateTime.Now;

        var pagamento = Pagamentos.FirstOrDefault();
        if (pagamento is not null)
        {
            if (novoStatus == StatusPedido.Cancelado && pagamento.Status == StatusPagamento.Aprovado)
                pagamento.Status = StatusPagamento.Estornado;
            if ((novoStatus == StatusPedido.Pago || novoStatus == StatusPedido.Entregue) && pagamento.Status == StatusPagamento.Pendente)
            {
                pagamento.Status = StatusPagamento.Aprovado;
                pagamento.PagoEm = DateTime.Now;
            }
        }
        return anterior;
    }
}

public class PedidoItem
{
    public int IdItemPedido { get; set; }
    public int IdPedido { get; set; }
    public int IdMedicamento { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public Pedido? Pedido { get; set; }
    public Medicamento? Medicamento { get; set; }
}

/// <summary>Linha do tempo do pedido (usada no rastreamento do site e do app).</summary>
public class PedidoHistoricoStatus
{
    public int IdHistorico { get; set; }
    public int IdPedido { get; set; }
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = string.Empty;
    public DateTime AlteradoEm { get; set; } = DateTime.Now;

    public Pedido? Pedido { get; set; }
}

public class Pagamento
{
    public int IdPagamento { get; set; }
    public int IdPedido { get; set; }
    public string Metodo { get; set; } = MetodoPagamento.Pix;
    public string Status { get; set; } = StatusPagamento.Pendente;
    public decimal Valor { get; set; }
    public string? CodigoTransacao { get; set; }
    public DateTime? PagoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;

    public Pedido? Pedido { get; set; }
}
