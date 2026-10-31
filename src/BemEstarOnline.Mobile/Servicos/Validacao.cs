using System.Globalization;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>Validações e máscaras iguais às do site (cadastro.js e app.js): CPF, data, telefone e CEP.</summary>
public static class Validacao
{
    public static string SoNumeros(string? texto) => new((texto ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>CPF com os dois dígitos verificadores (o mesmo cálculo do site e da API).</summary>
    public static bool CpfValido(string? cpf)
    {
        var n = SoNumeros(cpf);
        if (n.Length != 11 || n.Distinct().Count() == 1) return false;
        int Digito(int q)
        {
            var soma = 0;
            for (var i = 0; i < q; i++) soma += (n[i] - '0') * (q + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Digito(9) == n[9] - '0' && Digito(10) == n[10] - '0';
    }

    /// <summary>"20/05/1998" vira a data; null se a data não existir, for antes de 1900 ou no futuro.</summary>
    public static DateTime? Data(string? texto)
    {
        if (!DateTime.TryParseExact(texto?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return null;
        return d.Year < 1900 || d > DateTime.Today ? null : d;
    }

    public static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email) && System.Text.RegularExpressions.Regex.IsMatch(email.Trim(), @"^\S+@\S+\.\S+$");

    // Máscaras: só formatam quando a pessoa termina de digitar os números (assim o cursor não pula no meio).
    public static string MascaraCpf(string? t) => Completo(t, 11, "000.000.000-00");
    public static string MascaraData(string? t) => Completo(t, 8, "00/00/0000");
    public static string MascaraCep(string? t) => Completo(t, 8, "00000-000");
    public static string MascaraTelefone(string? t) =>
        SoNumeros(t).Length == 11 ? Aplicar(t, "(00) 00000-0000") : SoNumeros(t).Length == 10 ? Aplicar(t, "(00) 0000-0000") : t ?? "";

    private static string Completo(string? t, int digitos, string modelo) => SoNumeros(t).Length == digitos ? Aplicar(t, modelo) : t ?? "";

    private static string Aplicar(string? texto, string modelo)
    {
        var n = SoNumeros(texto);
        var i = 0;
        var saida = new System.Text.StringBuilder();
        foreach (var c in modelo)
        {
            if (i >= n.Length) break;
            saida.Append(c == '0' ? n[i++] : c);
        }
        return saida.ToString();
    }
}
