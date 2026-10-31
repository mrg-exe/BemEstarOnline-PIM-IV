using System.Globalization;
using SQLite;

namespace BemEstarOnline.Mobile.Modelos;

/*
 * Formatos de dados do app. Os nomes das propriedades são os mesmos dos DTOs da API
 * (o JSON chega em camelCase e é convertido automaticamente).
 * Produto, ItemCarrinho e PedidoPendente também viram tabelas no SQLite do celular (modo offline).
 */

/// <summary>Endereço do servidor sem o "/api/" (usado para montar a URL das fotos enviadas pelo painel).</summary>
public static class Servidor
{
    public static string Origem { get; set; } = "http://localhost:5088";
}

public static class Texto
{
    private static readonly CultureInfo Br = new("pt-BR");
    public static string Moeda(decimal valor) => valor.ToString("C", Br);
    public static string DataHora(DateTime d) => d.ToString("dd/MM/yyyy HH:mm", Br);
    public static string Data(DateTime? d) => d?.ToString("dd/MM/yyyy", Br) ?? "—";

    public static string Status(string codigo) => codigo switch
    {
        "AGUARDANDO_PAGAMENTO" => "Aguardando pagamento",
        "PAGO" => "Pagamento aprovado",
        "EM_SEPARACAO" => "Em separação",
        "SAIU_PARA_ENTREGA" => "Saiu para entrega",
        "ENTREGUE" => "Entregue",
        "CANCELADO" => "Cancelado",
        _ => codigo
    };
}

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = "";
    public string NomeExibicao { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefone { get; set; }
    public string Perfil { get; set; } = "";
    /// <summary>Senha provisória dada pelo administrador: o app pede uma senha nova antes de tudo (RF35).</summary>
    public bool TrocarSenha { get; set; }
    public bool EhEntregador => Perfil == "ENTREGADOR";
}

/// <summary>Respostas da API que só trazem um texto para mostrar (ex.: "esqueci minha senha").</summary>
public class RespostaMensagem
{
    public string Mensagem { get; set; } = "";
}

public class LoginResposta
{
    public string Token { get; set; } = "";
    public DateTime ExpiraEm { get; set; }
    public Usuario Usuario { get; set; } = new();
}

public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Icone { get; set; }
}

[Table("produtos_cache")]
public class Produto
{
    [PrimaryKey] public int IdMedicamento { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string CategoriaSlug { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? PrincipioAtivo { get; set; }
    public string Descricao { get; set; } = "";
    public string? Dosagem { get; set; }
    public string? Apresentacao { get; set; }
    public string Fabricante { get; set; } = "";
    public decimal Preco { get; set; }
    public bool ExigeReceita { get; set; }
    public bool UsoContinuo { get; set; }
    public bool FarmaciaPopular { get; set; }
    public int Estoque { get; set; }
    public bool Disponivel { get; set; }
    public string? ImagemUrl { get; set; }

    [Ignore] public bool TemFoto => !string.IsNullOrWhiteSpace(ImagemUrl);
    [Ignore] public bool SemFoto => !TemFoto;
    [Ignore] public string? FotoUrl => !TemFoto ? null
        : ImagemUrl!.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? ImagemUrl : Servidor.Origem + ImagemUrl;

    [Ignore] public string PrecoTexto => Texto.Moeda(Preco);
    [Ignore] public string Subtitulo => string.IsNullOrWhiteSpace(Apresentacao) ? Fabricante : $"{Apresentacao}, {Fabricante}";
    [Ignore] public string Icone => CategoriaSlug switch
    {
        "vitaminas" => "🍊", "higiene" => "🧼", "dermocosmeticos" => "🧴", "mamae-bebe" => "🍼",
        "primeiros-socorros" => "🩹", "aparelhos" => "🩺", _ => "💊"
    };
    [Ignore] public string Selos => string.Join("  ", new[]
    {
        ExigeReceita ? "Exige receita" : null,
        FarmaciaPopular ? "Farmácia Popular" : null,
        UsoContinuo ? "Uso contínuo" : null,
        Disponivel ? null : "Esgotado"
    }.Where(s => s is not null));
    [Ignore] public bool TemSelos => Selos.Length > 0;
    /// <summary>Texto da lista de escolha da assinatura (igual ao site).</summary>
    [Ignore] public string TextoEscolha => string.IsNullOrWhiteSpace(Apresentacao) ? $"{Nome} - {PrecoTexto}" : $"{Nome} ({Apresentacao}) - {PrecoTexto}";
    [Ignore] public string DescricaoAcessivel =>
        $"{Nome}, {Subtitulo}, {PrecoTexto}{(ExigeReceita ? ", exige receita" : "")}{(Disponivel ? "" : ", esgotado")}";
}

[Table("carrinho")]
public class ItemCarrinho
{
    [PrimaryKey] public int IdMedicamento { get; set; }
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    public int Quantidade { get; set; }
    public bool ExigeReceita { get; set; }

    [Ignore] public string PrecoTexto => $"{Texto.Moeda(Preco)} cada";
    [Ignore] public string SubtotalTexto => Texto.Moeda(Preco * Quantidade);
}

/// <summary>Pedido feito sem internet: fica guardado e é enviado quando a conexão volta.</summary>
[Table("pedidos_pendentes")]
public class PedidoPendente
{
    [PrimaryKey, AutoIncrement] public int IdLocal { get; set; }
    /// <summary>Dono do pedido: só é enviado com a conta de quem o fez.</summary>
    public int IdUsuario { get; set; }
    public string JsonPedido { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public bool Sincronizado { get; set; }
    public int? IdPedidoServidor { get; set; }
    public string? UltimoErro { get; set; }
}

public class Endereco
{
    public int IdEndereco { get; set; }
    public string? Apelido { get; set; }
    public string TextoCompleto { get; set; } = "";
    public bool Principal { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public override string ToString() => $"{Apelido ?? "Endereço"}: {TextoCompleto}";

    public string Titulo => $"{(string.IsNullOrWhiteSpace(Apelido) ? "Endereço" : Apelido)}{(Principal ? " (principal)" : "")}";
    public bool TemLocalizacao => Latitude is not null && Longitude is not null;
    public string DescricaoAcessivel => $"{Titulo}, {TextoCompleto}";
}

/// <summary>Resposta da API ao enviar a receita (RF39).</summary>
public class DeliveryResposta
{
    public int IdDelivery { get; set; }
    public string Status { get; set; } = "";
}

/// <summary>Assinatura de remédio de uso contínuo (RF38), com 10% de desconto.</summary>
public class Assinatura
{
    public int IdAssinatura { get; set; }
    public int IdMedicamento { get; set; }
    public string Medicamento { get; set; } = "";
    public string NomePaciente { get; set; } = "";
    public int Quantidade { get; set; }
    public int PeriodicidadeDias { get; set; }
    public decimal ValorPorEntrega { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ProximaEntrega { get; set; }

    public string Linha => $"{Quantidade} cx. a cada {PeriodicidadeDias} dias, para {NomePaciente}. {Texto.Moeda(ValorPorEntrega)} por entrega.";
    public string ProximaTexto => Ativa ? $"Próxima entrega: {Texto.Data(ProximaEntrega)}" : "";
    public string StatusTexto => Status switch { "ATIVA" => "Ativa", "PAUSADA" => "Pausada", "CANCELADA" => "Cancelada", _ => Status };
    public bool Ativa => Status == "ATIVA";
    public bool Pausada => Status == "PAUSADA";
    public bool PodeCancelar => Status != "CANCELADA";
    public string DescricaoAcessivel => $"{Medicamento}, {StatusTexto}, {Linha} {ProximaTexto}";
}

public class ItemQuantidade
{
    public int IdMedicamento { get; set; }
    public int Quantidade { get; set; }
}

public class ResumoValores
{
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public decimal Total { get; set; }
    public string? CupomAplicado { get; set; }
    public string? MensagemCupom { get; set; }
    public bool RequerReceita { get; set; }
    public decimal FaltaParaFreteGratis { get; set; }
}

public class PedidoCriar
{
    public List<ItemQuantidade> Itens { get; set; } = new();
    public int? IdEndereco { get; set; }
    public string? CodigoCupom { get; set; }
    public string MetodoPagamento { get; set; } = "PIX";
    public string? Observacao { get; set; }
    public string Canal { get; set; } = "APP";
}

public class PedidoResumo
{
    public int IdPedido { get; set; }
    public string Status { get; set; } = "";
    public string Canal { get; set; } = "";
    public decimal Total { get; set; }
    public int QuantidadeItens { get; set; }
    public DateTime CriadoEm { get; set; }

    public string Titulo => $"Pedido nº {IdPedido}";
    public string Linha => $"{Texto.DataHora(CriadoEm)}, {QuantidadeItens} item(ns), {Texto.Moeda(Total)}";
    public string StatusTexto => Texto.Status(Status);
    /// <summary>O que o leitor de tela (TalkBack/Narrador) fala ao chegar no pedido.</summary>
    public string DescricaoAcessivel => $"{Titulo}, {Linha}, {StatusTexto}";
}

public class PedidoItem
{
    public string NomeProduto { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal Subtotal { get; set; }
    public string Linha => $"{Quantidade}x {NomeProduto}";
    public string SubtotalTexto => Texto.Moeda(Subtotal);
}

public class Historico
{
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = "";
    public DateTime AlteradoEm { get; set; }
    public string Titulo => Texto.Status(StatusNovo);
    public string Quando => Texto.DataHora(AlteradoEm);
}

public class Pagamento
{
    public string Metodo { get; set; } = "";
    public string Status { get; set; } = "";
}

public class PedidoDetalhe
{
    public int IdPedido { get; set; }
    public string Status { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public decimal Total { get; set; }
    public bool RequerReceita { get; set; }
    public DateTime CriadoEm { get; set; }
    public Endereco? Endereco { get; set; }
    public Pagamento? Pagamento { get; set; }
    public List<PedidoItem> Itens { get; set; } = new();
    public List<Historico> Historico { get; set; } = new();
    public bool PodeCancelar { get; set; }
    public string? Entregador { get; set; }
    public bool ReceitaConferida { get; set; }
}

public class ErroApi
{
    public string? Mensagem { get; set; }
}
