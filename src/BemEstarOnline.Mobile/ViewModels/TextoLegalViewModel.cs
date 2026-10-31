using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BemEstarOnline.Mobile.ViewModels;

public enum TipoBloco { Titulo, Secao, Nota, Paragrafo, Item }

/// <summary>Pedaço de uma linha: texto normal, em negrito ou em itálico.</summary>
public record Trecho(string Texto, bool Negrito, bool Italico);

public record BlocoTexto(TipoBloco Tipo, List<Trecho> Trechos);

/// <summary>
/// Política de Privacidade e Termos de Uso no app (RF41). Lê os mesmos arquivos de docs/textos (que vão dentro do app)
/// e separa em blocos: títulos, parágrafos e itens de lista, com negrito e itálico. Funciona sem internet.
/// </summary>
public partial class TextoLegalViewModel : BaseViewModel, IQueryAttributable
{
    private static readonly Dictionary<string, (string Arquivo, string Titulo)> Documentos = new()
    {
        ["privacidade"] = ("textos/politica-de-privacidade.md", "Privacidade"),
        ["termos"] = ("textos/termos-de-uso.md", "Termos de Uso")
    };

    [ObservableProperty] private string titulo = "";
    [ObservableProperty] private List<BlocoTexto> blocos = new();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var chave = query.TryGetValue("doc", out var d) && Documentos.ContainsKey($"{d}") ? $"{d}" : "privacidade";
        var (arquivo, nome) = Documentos[chave];
        Titulo = nome;
        try
        {
            await using var fluxo = await FileSystem.OpenAppPackageFileAsync(arquivo);
            using var leitor = new StreamReader(fluxo);
            Blocos = Separar(await leitor.ReadToEndAsync());
        }
        catch (Exception)
        {
            Blocos = new() { new(TipoBloco.Paragrafo, new() { new("Não foi possível abrir o texto. Ele também está no site.", false, false) }) };
        }
    }

    /// <summary>Entende só o que os textos usam: "#", "##", "- " (lista), linhas que continuam a anterior, **negrito** e *itálico*.</summary>
    public static List<BlocoTexto> Separar(string markdown)
    {
        var blocos = new List<(TipoBloco Tipo, string Texto)>();
        var continuar = false;
        foreach (var bruta in markdown.Replace("\r", "").Split('\n'))
        {
            var linha = bruta.TrimEnd();
            if (linha.Length == 0) { continuar = false; continue; }

            if (linha.StartsWith("## ")) { blocos.Add((TipoBloco.Secao, linha[3..])); continuar = false; }
            else if (linha.StartsWith("# ")) { blocos.Add((TipoBloco.Titulo, linha[2..])); continuar = false; }
            else if (linha.StartsWith("- ")) { blocos.Add((TipoBloco.Item, linha[2..])); continuar = true; }
            else if (continuar && blocos.Count > 0 && (blocos[^1].Tipo != TipoBloco.Item || char.IsWhiteSpace(bruta[0])))
            {
                // Linha que continua o parágrafo de cima (ou o item da lista, se vier com recuo).
                var (tipo, texto) = blocos[^1];
                blocos[^1] = (tipo, $"{texto} {linha.Trim()}");
            }
            else
            {
                var nota = linha.StartsWith('*') && linha.EndsWith('*') && !linha.StartsWith("**");
                blocos.Add((nota ? TipoBloco.Nota : TipoBloco.Paragrafo, nota ? linha.Trim('*') : linha));
                continuar = true;
            }
        }
        return blocos.Select(b => new BlocoTexto(b.Tipo, Trechos(b.Texto))).ToList();
    }

    private static List<Trecho> Trechos(string texto) =>
        Regex.Split(texto, @"(\*\*[^*]+\*\*|\*[^*]+\*)")
            .Where(p => p.Length > 0)
            .Select(p => p.StartsWith("**") && p.EndsWith("**") && p.Length > 4 ? new Trecho(p[2..^2], true, false)
                       : p.StartsWith('*') && p.EndsWith('*') && p.Length > 2 ? new Trecho(p[1..^1], false, true)
                       : new Trecho(p, false, false))
            .ToList();
}
