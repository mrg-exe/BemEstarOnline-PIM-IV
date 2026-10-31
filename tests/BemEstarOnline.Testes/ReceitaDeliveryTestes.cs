using System.Text;
using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Aplicacao.Servicos;
using BemEstarOnline.Api.Dominio;

namespace BemEstarOnline.Testes;

/// <summary>Foto da receita (RF39): só JPG, PNG ou PDF de verdade, até 5 MB, guardada na pasta privada.</summary>
public class ReceitaDeliveryTestes
{
    private readonly DeliveryFalso _solicitacoes = new();
    private readonly ArquivosFalsos _arquivos = new();
    private readonly DeliveryServico _servico;

    private static readonly DeliveryCriarDto Pedido = new("Ana", "(11) 98765-4321", "Rua Guapiara, 120, Cajamar", "Ver receita anexada.");
    private static readonly byte[] InicioJpg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 };
    private static readonly byte[] InicioPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 };

    public ReceitaDeliveryTestes() => _servico = new DeliveryServico(_solicitacoes, new UnidadeDeTrabalhoFalsa(), _arquivos);

    private static ArquivoRecebido Arquivo(string nome, byte[] bytes) => new(nome, bytes.Length, () => new MemoryStream(bytes));

    [Theory]
    [InlineData("receita.jpg", ".jpg")]
    [InlineData("RECEITA.JPEG", ".jpg")]
    public async Task Foto_jpg_e_guardada_com_nome_aleatorio(string nome, string extensao)
    {
        var dto = await _servico.CriarAsync(null, Pedido, Arquivo(nome, InicioJpg));

        Assert.Matches("^[0-9a-f]{32}" + extensao.Replace(".", "\\.") + "$", dto.ReceitaArquivo);
        Assert.Equal(InicioJpg, _arquivos.Guardados[dto.ReceitaArquivo!]);
    }

    [Fact]
    public async Task Png_e_pdf_tambem_sao_aceitos()
    {
        var png = await _servico.CriarAsync(null, Pedido, Arquivo("foto.png", InicioPng));
        var pdf = await _servico.CriarAsync(null, Pedido, Arquivo("receita.pdf", Encoding.ASCII.GetBytes("%PDF-1.4 ...")));

        Assert.EndsWith(".png", png.ReceitaArquivo);
        Assert.EndsWith(".pdf", pdf.ReceitaArquivo);
    }

    [Fact]
    public async Task Sem_receita_a_solicitacao_e_criada_sem_arquivo()
    {
        var dto = await _servico.CriarAsync(null, Pedido);

        Assert.Null(dto.ReceitaArquivo);
        Assert.Empty(_arquivos.Guardados);
    }

    [Fact]
    public async Task Texto_disfarcado_de_jpg_e_recusado()
    {
        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.CriarAsync(null, Pedido, Arquivo("receita.jpg", Encoding.ASCII.GetBytes("isto não é uma foto"))));

        Assert.Contains("não é uma foto", erro.Message);
        Assert.Empty(_arquivos.Guardados);
        Assert.Empty(_solicitacoes.Solicitacoes);
    }

    [Theory]
    [InlineData("programa.exe")]
    [InlineData("receita.gif")]
    [InlineData("receita")]
    public async Task Tipo_de_arquivo_nao_permitido_e_recusado(string nome) =>
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.CriarAsync(null, Pedido, Arquivo(nome, InicioJpg)));

    [Fact]
    public async Task Arquivo_acima_de_5_mb_e_recusado()
    {
        var grande = new byte[DeliveryServico.TamanhoMaximoReceita + 1];
        InicioJpg.CopyTo(grande, 0);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.CriarAsync(null, Pedido, Arquivo("grande.jpg", grande)));

        Assert.Contains("5 MB", erro.Message);
    }

    [Fact]
    public async Task Arquivo_vazio_e_recusado() =>
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _servico.CriarAsync(null, Pedido, Arquivo("receita.jpg", Array.Empty<byte>())));

    [Fact]
    public async Task Equipe_abre_a_receita_guardada()
    {
        var dto = await _servico.CriarAsync(null, Pedido, Arquivo("receita.jpg", InicioJpg));

        var arquivo = await _servico.AbrirReceitaAsync(dto.IdDelivery);

        using var copia = new MemoryStream();
        await arquivo.Conteudo.CopyToAsync(copia);
        Assert.Equal(InicioJpg, copia.ToArray());
    }

    [Fact]
    public async Task Solicitacao_sem_receita_avisa_que_nao_tem_arquivo()
    {
        var dto = await _servico.CriarAsync(null, Pedido);

        var erro = await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.AbrirReceitaAsync(dto.IdDelivery));

        Assert.Contains("não tem receita", erro.Message);
    }
}
