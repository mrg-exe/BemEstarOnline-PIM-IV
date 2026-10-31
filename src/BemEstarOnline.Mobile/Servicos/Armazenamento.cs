using System.Text.Json;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// Guarda configurações simples (endereço da API, sessão, acessibilidade) num arquivo JSON
/// na pasta de dados do app. Funciona igual no Android e no Windows (inclusive sem instalador).
/// </summary>
public class Armazenamento
{
    private readonly string _arquivo = Path.Combine(FileSystem.AppDataDirectory, "configuracoes.json");
    private readonly object _trava = new();
    private Dictionary<string, string> _valores;

    public Armazenamento()
    {
        try
        {
            _valores = File.Exists(_arquivo)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_arquivo)) ?? new()
                : new();
        }
        catch { _valores = new(); }
    }

    public string? Ler(string chave) { lock (_trava) return _valores.TryGetValue(chave, out var v) ? v : null; }

    public void Gravar(string chave, string? valor)
    {
        lock (_trava)
        {
            if (valor is null) _valores.Remove(chave); else _valores[chave] = valor;
            Directory.CreateDirectory(Path.GetDirectoryName(_arquivo)!);
            File.WriteAllText(_arquivo, JsonSerializer.Serialize(_valores));
        }
    }
}
