namespace BemEstarOnline.Api.Dominio;

/// <summary>Perfis de acesso do sistema (usados nas regras de autorização da API).</summary>
public static class Perfis
{
    public const string Cliente = "CLIENTE";
    public const string Atendente = "ATENDENTE";
    public const string Admin = "ADMIN";
    public const string Entregador = "ENTREGADOR";

    /// <summary>Perfis da equipe interna da farmácia (podem acessar a área administrativa).</summary>
    public const string Equipe = Admin + "," + Atendente;

    public static bool EhEquipe(string perfil) => perfil == Admin || perfil == Atendente;

    /// <summary>Perfis que o administrador cadastra na gestão da equipe (RF35). Cliente se cadastra sozinho.</summary>
    public static readonly IReadOnlyList<string> Funcionarios = new[] { Atendente, Entregador, Admin };
}

/// <summary>Situações possíveis de um pedido e as mudanças permitidas entre elas.</summary>
public static class StatusPedido
{
    public const string AguardandoPagamento = "AGUARDANDO_PAGAMENTO";
    public const string Pago = "PAGO";
    public const string EmSeparacao = "EM_SEPARACAO";
    public const string SaiuParaEntrega = "SAIU_PARA_ENTREGA";
    public const string Entregue = "ENTREGUE";
    public const string Cancelado = "CANCELADO";

    public static readonly IReadOnlyList<string> Todos = new[]
    {
        AguardandoPagamento, Pago, EmSeparacao, SaiuParaEntrega, Entregue, Cancelado
    };

    /// <summary>Pedidos nestes status ainda precisam de alguma ação da farmácia.</summary>
    public static readonly IReadOnlyList<string> EmAberto = new[]
    {
        AguardandoPagamento, Pago, EmSeparacao, SaiuParaEntrega
    };

    private static readonly Dictionary<string, string[]> Transicoes = new()
    {
        [AguardandoPagamento] = new[] { Pago, EmSeparacao, Cancelado },
        [Pago] = new[] { EmSeparacao, Cancelado },
        [EmSeparacao] = new[] { SaiuParaEntrega, Cancelado },
        [SaiuParaEntrega] = new[] { Entregue },
        [Entregue] = Array.Empty<string>(),
        [Cancelado] = Array.Empty<string>(),
    };

    public static bool PodeMudar(string atual, string novo) =>
        Transicoes.TryGetValue(atual, out var permitidos) && permitidos.Contains(novo);

    public static IReadOnlyList<string> ProximosPossiveis(string atual) =>
        Transicoes.TryGetValue(atual, out var permitidos) ? permitidos : Array.Empty<string>();
}

public static class MetodoPagamento
{
    public const string Pix = "PIX";
    public const string CartaoCredito = "CARTAO_CREDITO";
    public const string CartaoDebito = "CARTAO_DEBITO";
    /// <summary>Pagamento em dinheiro na entrega.</summary>
    public const string Dinheiro = "DINHEIRO";
    public static readonly IReadOnlyList<string> Todos = new[] { Pix, CartaoCredito, CartaoDebito, Dinheiro };
}

public static class StatusPagamento
{
    public const string Pendente = "PENDENTE";
    public const string Aprovado = "APROVADO";
    public const string Estornado = "ESTORNADO";
}

public static class StatusAssinatura
{
    public const string Ativa = "ATIVA";
    public const string Pausada = "PAUSADA";
    public const string Cancelada = "CANCELADA";
    public static readonly IReadOnlyList<string> Todos = new[] { Ativa, Pausada, Cancelada };
}

public static class StatusDelivery
{
    public const string Recebido = "RECEBIDO";
    public const string EmAnalise = "EM_ANALISE";
    public const string EmRota = "EM_ROTA";
    public const string Concluido = "CONCLUIDO";
    public const string Cancelado = "CANCELADO";
    public static readonly IReadOnlyList<string> Todos = new[] { Recebido, EmAnalise, EmRota, Concluido, Cancelado };
}

/// <summary>Tipos de manifestação da ouvidoria (RF30).</summary>
public static class TipoOuvidoria
{
    public const string Discriminacao = "DISCRIMINACAO";
    public const string Atendimento = "ATENDIMENTO";
    public const string Entrega = "ENTREGA";
    public const string Sugestao = "SUGESTAO";
    public const string Elogio = "ELOGIO";
    public static readonly IReadOnlyList<string> Todos = new[] { Discriminacao, Atendimento, Entrega, Sugestao, Elogio };
}

/// <summary>Tipos de discriminação que podem ser denunciados.</summary>
public static class SubtipoDiscriminacao
{
    public static readonly IReadOnlyList<string> Todos = new[] { "RACIAL", "RELIGIOSA", "GENERO", "DEFICIENCIA", "IDADE", "OUTRA" };
}

public static class StatusOuvidoria
{
    public const string Aberta = "ABERTA";
    public const string EmAnalise = "EM_ANALISE";
    public const string Respondida = "RESPONDIDA";
    public const string Encerrada = "ENCERRADA";
    public static readonly IReadOnlyList<string> Todos = new[] { Aberta, EmAnalise, Respondida, Encerrada };
    /// <summary>Ainda esperam ação da equipe.</summary>
    public static readonly IReadOnlyList<string> EmAberto = new[] { Aberta, EmAnalise };
}

public static class TipoCupom
{
    public const string Percentual = "PERCENTUAL";
    public const string ValorFixo = "VALOR_FIXO";
    public const string FreteGratis = "FRETE_GRATIS";
}

/// <summary>Canal por onde o pedido foi feito.</summary>
public static class CanalVenda
{
    public const string Site = "SITE";
    public const string App = "APP";
}

/// <summary>Regras comerciais fixas da farmácia.</summary>
public static class RegrasComerciais
{
    public const decimal ValorFrete = 8.90m;
    public const decimal FreteGratisAPartirDe = 99.00m;
    public const decimal DescontoAssinaturaPercentual = 10m;
    public static readonly int[] PeriodicidadesAssinatura = { 15, 30, 60, 90 };

    /// <summary>Endereço da loja (Centro de Cajamar/SP): ponto de partida das entregas no mapa.</summary>
    public const double LojaLatitude = -23.35566;
    public const double LojaLongitude = -46.87706;

    public static decimal CalcularFrete(decimal subtotal) =>
        subtotal >= FreteGratisAPartirDe ? 0m : ValorFrete;
}
