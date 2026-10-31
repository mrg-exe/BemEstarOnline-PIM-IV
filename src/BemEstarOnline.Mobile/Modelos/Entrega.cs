using System.Globalization;

namespace BemEstarOnline.Mobile.Modelos;

/// <summary>Posição do entregador para o mapa do cliente.</summary>
public class Rastreio
{
    public int IdPedido { get; set; }
    public string Status { get; set; } = "";
    public string? Entregador { get; set; }
    public double? EntregadorLatitude { get; set; }
    public double? EntregadorLongitude { get; set; }
    public DateTime? LocalizacaoEm { get; set; }
    public double LojaLatitude { get; set; }
    public double LojaLongitude { get; set; }
    public double? DestinoLatitude { get; set; }
    public double? DestinoLongitude { get; set; }
}

/// <summary>Pedido visto pelo entregador.</summary>
public class Entrega
{
    public int IdPedido { get; set; }
    public string Status { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string? Telefone { get; set; }
    public string EnderecoTexto { get; set; } = "";
    public double? DestinoLatitude { get; set; }
    public double? DestinoLongitude { get; set; }
    public bool RequerReceita { get; set; }
    public bool ReceitaConferida { get; set; }
    public decimal Total { get; set; }
    public string FormaPagamento { get; set; } = "";
    public bool CobrarNaEntrega { get; set; }
    public string? Observacao { get; set; }
    public List<PedidoItem> Itens { get; set; } = new();
    public DateTime CriadoEm { get; set; }
    public bool MinhaEntrega { get; set; }

    public string Titulo => $"Pedido nº {IdPedido}";
    public string StatusTexto => MinhaEntrega ? Texto.Status(Status) : "Disponível para pegar";
    public string AvisoReceita => RequerReceita ? "EXIGE RECEITA: confira a receita antes de entregar" : "Não exige receita";
    public string Cobranca => CobrarNaEntrega ? $"Receber {Texto.Moeda(Total)} em dinheiro" : $"Já pago ({Texto.Moeda(Total)})";
    public bool TemObservacao => !string.IsNullOrWhiteSpace(Observacao);
    public string ResumoItens => string.Join(", ", Itens.Select(i => $"{i.Quantidade}x {i.NomeProduto}"));
}

/// <summary>
/// Mapa (Leaflet + OpenStreetMap, sem chave de API) exibido num WebView.
/// A página fica no site (frontend/mapa-app.html) e é aberta pelo endereço do servidor:
/// o OpenStreetMap bloqueia mapas abertos como HTML solto, sem saber de qual site vieram.
/// </summary>
public static class MapaHtml
{
    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>Endereço da página do mapa com a farmácia, o cliente e o entregador (se houver).</summary>
    public static string Endereco(double lojaLat, double lojaLng, double? destinoLat, double? destinoLng, double? entLat, double? entLng)
    {
        var url = $"{Servidor.Origem}/mapa-app.html?loja={N(lojaLat)},{N(lojaLng)}";
        if (destinoLat.HasValue && destinoLng.HasValue) url += $"&destino={N(destinoLat.Value)},{N(destinoLng.Value)}";
        if (entLat.HasValue && entLng.HasValue) url += $"&entregador={N(entLat.Value)},{N(entLng.Value)}";
        return url;
    }

    /// <summary>Comando JavaScript para mover o ponto do entregador sem recarregar o mapa.</summary>
    public static string Mover(double lat, double lng) => $"atualizar({N(lat)},{N(lng)});";

    public static double DistanciaKm(double a1, double o1, double a2, double o2)
    {
        static double Rad(double x) => x * Math.PI / 180;
        var d = Math.Pow(Math.Sin(Rad(a2 - a1) / 2), 2) + Math.Cos(Rad(a1)) * Math.Cos(Rad(a2)) * Math.Pow(Math.Sin(Rad(o2 - o1) / 2), 2);
        return 2 * 6371 * Math.Asin(Math.Sqrt(d));
    }
}
