using BemEstarOnline.Api.Aplicacao.Servicos;

namespace BemEstarOnline.Testes;

/// <summary>CPF pelos dígitos verificadores (regra da Receita Federal), usado no cadastro e na equipe.</summary>
public class ValidadorCpfTestes
{
    [Theory]
    [InlineData("529.982.247-25")] // com pontos e traço
    [InlineData("52998224725")]    // só números
    [InlineData("111.444.777-35")]
    [InlineData(" 529 982 247 25 ")] // espaços são ignorados
    public void Aceita_cpf_valido(string cpf) => Assert.True(ValidadorCpf.EhValido(cpf));

    [Theory]
    [InlineData("529.982.247-24")] // último dígito errado
    [InlineData("529.982.247-15")] // penúltimo dígito errado
    [InlineData("111.111.111-11")] // todos iguais passam na conta, mas não existem
    [InlineData("000.000.000-00")]
    [InlineData("5299822472")]     // 10 números
    [InlineData("529982247251")]   // 12 números
    [InlineData("")]
    [InlineData("abc.def.ghi-jk")]
    public void Recusa_cpf_invalido(string cpf) => Assert.False(ValidadorCpf.EhValido(cpf));

    [Theory]
    [InlineData("52998224725", "529.982.247-25")]
    [InlineData("529.982.247-25", "529.982.247-25")]
    [InlineData("123", "123")] // incompleto: devolve como veio
    public void Formata_com_pontos_e_traco(string entrada, string esperado) =>
        Assert.Equal(esperado, ValidadorCpf.Formatar(entrada));
}
