namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// "Caixa-preta" do app: se ele fechar sozinho (ou algo importante falhar), a mensagem de erro fica guardada num arquivo
/// e aparece na próxima vez que o app abrir, com um botão para copiar. Serve para descobrir problemas que só acontecem
/// no celular de verdade, sem cabo nem ferramenta de programador.
/// </summary>
public static class RegistroDeErros
{
    private static string Arquivo => Path.Combine(FileSystem.AppDataDirectory, "ultimo-erro.txt");

    public static void Iniciar()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Gravar(e.ExceptionObject as Exception, "o app fechou sozinho");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Gravar(e.Exception, "tarefa em segundo plano");
            e.SetObserved(); // não derruba o app por causa de uma tarefa de fundo
        };
    }

    public static void Gravar(Exception? erro, string onde)
    {
        try
        {
            File.WriteAllText(Arquivo,
                $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} | {onde} | {DeviceInfo.Platform} {DeviceInfo.VersionString} | " +
                $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}\n{erro}");
        }
        catch { /* sem como gravar: nada a fazer */ }
    }

    /// <summary>Devolve o erro guardado (e apaga o arquivo), ou null se o app não teve problema.</summary>
    public static string? LerEApagar()
    {
        try
        {
            if (!File.Exists(Arquivo)) return null;
            var texto = File.ReadAllText(Arquivo);
            File.Delete(Arquivo);
            return texto;
        }
        catch { return null; }
    }
}
