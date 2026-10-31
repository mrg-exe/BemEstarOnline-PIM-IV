using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;

namespace BemEstarOnline.Testes;

/// <summary>Cálculo do desconto (Cupom.Aplicar) e validade do cupom (Cupom.MotivoInvalido).</summary>
public class CupomTestes
{
    private static readonly DateTime Hoje = new(2026, 11, 7, 10, 0, 0);

    private static Cupom NovoCupom(string tipo, decimal valor) => new()
    {
        Codigo = "TESTE",
        Tipo = tipo,
        Valor = valor,
        DataInicio = Hoje.AddDays(-10),
        Ativo = true
    };

    [Fact]
    public void Percentual_da_desconto_sobre_os_produtos_e_mantem_o_frete()
    {
        var (desconto, frete) = NovoCupom(TipoCupom.Percentual, 10).Aplicar(subtotal: 150m, frete: 8.90m);

        Assert.Equal(15.00m, desconto);
        Assert.Equal(8.90m, frete);
    }

    [Fact]
    public void Percentual_arredonda_para_centavos()
    {
        var (desconto, _) = NovoCupom(TipoCupom.Percentual, 10).Aplicar(subtotal: 33.33m, frete: 0m);

        Assert.Equal(3.33m, desconto);
    }

    [Theory]
    [InlineData(50, 20)] // compra maior que o cupom: desconta o valor do cupom
    [InlineData(15, 15)] // compra menor: o desconto nunca passa do valor dos produtos
    public void Valor_fixo_nunca_desconta_mais_do_que_a_compra(decimal subtotal, decimal descontoEsperado)
    {
        var (desconto, frete) = NovoCupom(TipoCupom.ValorFixo, 20).Aplicar(subtotal, frete: 8.90m);

        Assert.Equal(descontoEsperado, desconto);
        Assert.Equal(8.90m, frete);
    }

    [Fact]
    public void Frete_gratis_zera_o_frete_e_nao_desconta_produtos()
    {
        var (desconto, frete) = NovoCupom(TipoCupom.FreteGratis, 0).Aplicar(subtotal: 60m, frete: 8.90m);

        Assert.Equal(0m, desconto);
        Assert.Equal(0m, frete);
    }

    [Fact]
    public void Cupom_valido_nao_tem_motivo_de_recusa() =>
        Assert.Null(NovoCupom(TipoCupom.Percentual, 10).MotivoInvalido(100m, Hoje));

    [Fact]
    public void Recusa_cupom_desativado()
    {
        var cupom = NovoCupom(TipoCupom.Percentual, 10);
        cupom.Ativo = false;
        Assert.Equal("Este cupom não está mais ativo.", cupom.MotivoInvalido(100m, Hoje));
    }

    [Fact]
    public void Recusa_cupom_vencido()
    {
        var cupom = NovoCupom(TipoCupom.Percentual, 10);
        cupom.DataFim = Hoje.AddDays(-1);
        Assert.Equal("Este cupom expirou.", cupom.MotivoInvalido(100m, Hoje));
    }

    [Fact]
    public void Recusa_cupom_que_ainda_nao_comecou()
    {
        var cupom = NovoCupom(TipoCupom.Percentual, 10);
        cupom.DataInicio = Hoje.AddDays(1);
        Assert.Equal("Este cupom ainda não começou a valer.", cupom.MotivoInvalido(100m, Hoje));
    }

    [Fact]
    public void Recusa_cupom_que_atingiu_o_limite_de_usos()
    {
        var cupom = NovoCupom(TipoCupom.Percentual, 10);
        cupom.LimiteUso = 50;
        cupom.UsosRealizados = 50;
        Assert.Equal("Este cupom atingiu o limite de usos.", cupom.MotivoInvalido(100m, Hoje));
    }

    [Fact]
    public void Recusa_compra_abaixo_do_valor_minimo()
    {
        var cupom = NovoCupom(TipoCupom.FreteGratis, 0);
        cupom.ValorMinimoPedido = 99m;

        Assert.Contains("a partir de R$", cupom.MotivoInvalido(98.99m, Hoje));
        Assert.Null(cupom.MotivoInvalido(99m, Hoje)); // exatamente o mínimo já vale
    }
}
