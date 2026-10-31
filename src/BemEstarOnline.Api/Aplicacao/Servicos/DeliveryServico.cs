using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>Pedido rápido por texto/receita: a farmácia analisa e retorna com o orçamento.</summary>
public class DeliveryServico : IDeliveryServico
{
    private readonly IDeliveryRepositorio _solicitacoes;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IArquivosPrivados _arquivos;

    /// <summary>Tamanho máximo da foto/PDF da receita (RF39).</summary>
    public const long TamanhoMaximoReceita = 5 * 1024 * 1024;
    private static readonly string[] ExtensoesAceitas = { ".jpg", ".jpeg", ".png", ".pdf" };

    public DeliveryServico(IDeliveryRepositorio solicitacoes, IUnidadeDeTrabalho uow, IArquivosPrivados arquivos)
    {
        _solicitacoes = solicitacoes;
        _uow = uow;
        _arquivos = arquivos;
    }

    public async Task<DeliveryDto> CriarAsync(int? idUsuario, DeliveryCriarDto dto, ArquivoRecebido? receita = null)
    {
        // A receita é conferida e guardada antes; se o banco falhar, o arquivo é apagado.
        var nomeGuardado = receita is null ? null : await GuardarReceitaAsync(receita);
        try
        {
            var solicitacao = new DeliverySolicitacao
            {
                IdUsuario = idUsuario,
                NomeCliente = dto.NomeCliente.Trim(),
                Telefone = dto.Telefone.Trim(),
                EnderecoTexto = dto.EnderecoTexto.Trim(),
                Observacao = dto.Observacao.Trim(),
                ReceitaArquivo = nomeGuardado
            };
            _solicitacoes.Adicionar(solicitacao);
            await _uow.SalvarAsync();
            return solicitacao.ParaDto();
        }
        catch
        {
            if (nomeGuardado is not null) _arquivos.ApagarReceita(nomeGuardado);
            throw;
        }
    }

    /// <summary>
    /// Aceita JPG, PNG ou PDF de até 5 MB. O tipo é conferido pelos primeiros bytes do arquivo (a "assinatura"),
    /// não só pelo nome: um programa renomeado para "receita.jpg" é recusado.
    /// </summary>
    private async Task<string> GuardarReceitaAsync(ArquivoRecebido receita)
    {
        if (!ExtensoesAceitas.Contains(Path.GetExtension(receita.Nome).ToLowerInvariant()))
            throw new RegraDeNegocioException("Envie a receita como foto (JPG ou PNG) ou PDF.");
        if (receita.Tamanho == 0) throw new RegraDeNegocioException("O arquivo da receita está vazio.");
        if (receita.Tamanho > TamanhoMaximoReceita) throw new RegraDeNegocioException("A receita pode ter no máximo 5 MB.");

        using var memoria = new MemoryStream();
        await using (var origem = receita.Abrir()) await origem.CopyToAsync(memoria);
        if (memoria.Length > TamanhoMaximoReceita) throw new RegraDeNegocioException("A receita pode ter no máximo 5 MB.");

        var extensao = TipoPeloConteudo(memoria.GetBuffer().AsSpan(0, (int)memoria.Length))
                       ?? throw new RegraDeNegocioException("O arquivo não é uma foto (JPG ou PNG) nem um PDF válido.");
        memoria.Position = 0;
        return await _arquivos.GuardarReceitaAsync(memoria, extensao);
    }

    private static string? TipoPeloConteudo(ReadOnlySpan<byte> b)
    {
        if (b.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ".jpg";
        if (b.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (b.StartsWith("%PDF-"u8)) return ".pdf";
        return null;
    }

    public async Task<ArquivoGuardado> AbrirReceitaAsync(int idDelivery)
    {
        var solicitacao = await _solicitacoes.ObterPorIdAsync(idDelivery)
                          ?? throw new NaoEncontradoException("Solicitação não encontrada.");
        if (solicitacao.ReceitaArquivo is null) throw new NaoEncontradoException("Esta solicitação não tem receita anexada.");
        return _arquivos.AbrirReceita(solicitacao.ReceitaArquivo)
               ?? throw new NaoEncontradoException("O arquivo desta receita não está guardado (solicitação feita antes do envio de arquivos).");
    }

    public async Task<List<DeliveryDto>> ListarDoUsuarioAsync(int idUsuario) =>
        (await _solicitacoes.ListarPorUsuarioAsync(idUsuario)).Select(d => d.ParaDto()).ToList();

    public async Task<List<DeliveryDto>> ListarTodasAsync() =>
        (await _solicitacoes.ListarTodasAsync()).Select(d => d.ParaDto()).ToList();

    public async Task<DeliveryDto> AlterarStatusAsync(int idDelivery, string status)
    {
        if (!StatusDelivery.Todos.Contains(status)) throw new RegraDeNegocioException("Situação inválida.");
        var solicitacao = await _solicitacoes.ObterPorIdAsync(idDelivery)
                          ?? throw new NaoEncontradoException("Solicitação não encontrada.");
        solicitacao.Status = status;
        await _uow.SalvarAsync();
        return solicitacao.ParaDto();
    }
}
