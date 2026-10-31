using BemEstarOnline.Api.Aplicacao.Interfaces;

namespace BemEstarOnline.Api.Infraestrutura.Arquivos;

/// <summary>
/// Guarda as receitas em ArquivosPrivados/receitas (RF39). A pasta fica fora do wwwroot e da pasta do site,
/// então nenhum arquivo dela tem endereço público: só sai pela rota da equipe GET /api/admin/delivery/{id}/receita.
/// </summary>
public class ArquivosPrivados : IArquivosPrivados
{
    private static readonly Dictionary<string, string> Tipos = new()
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".pdf"] = "application/pdf"
    };

    private readonly string _pastaReceitas;

    public ArquivosPrivados(string pastaBase)
    {
        _pastaReceitas = Path.Combine(pastaBase, "receitas");
        Directory.CreateDirectory(_pastaReceitas);
    }

    public async Task<string> GuardarReceitaAsync(Stream conteudo, string extensao)
    {
        if (!Tipos.ContainsKey(extensao)) throw new ArgumentException("Tipo de arquivo não permitido.", nameof(extensao));
        var nome = $"{Guid.NewGuid():N}{extensao}";
        await using var arquivo = new FileStream(Path.Combine(_pastaReceitas, nome), FileMode.CreateNew, FileAccess.Write);
        await conteudo.CopyToAsync(arquivo);
        return nome;
    }

    public ArquivoGuardado? AbrirReceita(string nome)
    {
        var caminho = Caminho(nome);
        if (caminho is null || !File.Exists(caminho)) return null;
        return new ArquivoGuardado(File.OpenRead(caminho), Tipos[Path.GetExtension(caminho)]);
    }

    public void ApagarReceita(string nome)
    {
        var caminho = Caminho(nome);
        if (caminho is not null && File.Exists(caminho)) File.Delete(caminho);
    }

    /// <summary>
    /// Só aceita nomes no formato que a própria API cria (GUID + extensão conhecida). Isso impede nomes como
    /// "../appsettings.json" de sair da pasta. Solicitações antigas, que guardavam só o nome do arquivo, dão null.
    /// </summary>
    private string? Caminho(string nome)
    {
        var arquivo = Path.GetFileName(nome);
        if (arquivo != nome || !Tipos.ContainsKey(Path.GetExtension(arquivo))
            || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(arquivo), "N", out _)) return null;
        return Path.Combine(_pastaReceitas, arquivo);
    }
}
