namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// Alto contraste e letra grande (WCAG). Troca, em tempo real, os recursos usados
/// com DynamicResource em todas as telas. A escolha fica salva no aparelho.
/// </summary>
public class AcessibilidadeServico
{
    private readonly Armazenamento _armazenamento;
    public AcessibilidadeServico(Armazenamento armazenamento) => _armazenamento = armazenamento;

    public bool AltoContraste => _armazenamento.Ler("alto_contraste") == "1";
    public bool LetraGrande => _armazenamento.Ler("letra_grande") == "1";

    public void Definir(bool altoContraste, bool letraGrande)
    {
        _armazenamento.Gravar("alto_contraste", altoContraste ? "1" : "0");
        _armazenamento.Gravar("letra_grande", letraGrande ? "1" : "0");
        Aplicar();
    }

    public void Aplicar()
    {
        var r = Application.Current?.Resources;
        if (r is null) return;

        if (AltoContraste)
        {
            r["CorFundo"] = Colors.Black;
            r["CorCartao"] = Colors.Black;
            r["CorTexto"] = Colors.White;
            r["CorTextoSuave"] = Colors.White;
            r["CorBorda"] = Colors.White;
            r["CorPrimaria"] = Color.FromArgb("#FFE600");
            r["CorPrimariaEscura"] = Color.FromArgb("#FFE600");
            r["CorTextoBotao"] = Colors.Black;
            r["CorTarja"] = Color.FromArgb("#FF8A80");
            r["CorFolha"] = Color.FromArgb("#FFE600");
            r["CorBolinhaChave"] = Colors.White; // senão o Switch desligado some no fundo preto
        }
        else
        {
            r["CorFundo"] = Color.FromArgb("#F4F8F7");
            r["CorCartao"] = Colors.White;
            r["CorTexto"] = Color.FromArgb("#0D3550");
            r["CorTextoSuave"] = Color.FromArgb("#4A6378");
            r["CorBorda"] = Color.FromArgb("#D3E1DE");
            r["CorPrimaria"] = Color.FromArgb("#00807C");
            r["CorPrimariaEscura"] = Color.FromArgb("#00625F");
            r["CorTextoBotao"] = Colors.White;
            r["CorTarja"] = Color.FromArgb("#C8281E");
            r["CorFolha"] = Color.FromArgb("#3F7A22");
            r["CorBolinhaChave"] = Color.FromArgb("#4A6378");
        }

        r["TamanhoTexto"] = LetraGrande ? 20d : 16d;
        r["TamanhoTitulo"] = LetraGrande ? 28d : 22d;
        r["AlturaBotao"] = LetraGrande ? 60d : 50d;
    }
}
