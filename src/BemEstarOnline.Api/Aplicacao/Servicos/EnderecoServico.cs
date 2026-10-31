using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

public class EnderecoServico : IEnderecoServico
{
    private readonly IEnderecoRepositorio _enderecos;
    private readonly IUnidadeDeTrabalho _uow;

    public EnderecoServico(IEnderecoRepositorio enderecos, IUnidadeDeTrabalho uow)
    {
        _enderecos = enderecos;
        _uow = uow;
    }

    public async Task<List<EnderecoDto>> ListarAsync(int idUsuario) =>
        (await _enderecos.ListarPorUsuarioAsync(idUsuario)).Select(e => e.ParaDto()).ToList();

    public async Task<EnderecoDto> CriarAsync(int idUsuario, EnderecoSalvarDto dto)
    {
        var existentes = await _enderecos.ListarPorUsuarioAsync(idUsuario);
        var endereco = NovoEndereco(idUsuario, dto, existentes.Count == 0);
        if (endereco.Principal) foreach (var e in existentes) e.Principal = false;

        _enderecos.Adicionar(endereco);
        await _uow.SalvarAsync();
        return endereco.ParaDto();
    }

    public async Task RemoverAsync(int idUsuario, int idEndereco)
    {
        var endereco = await _enderecos.ObterPorIdAsync(idEndereco);
        if (endereco is null || endereco.IdUsuario != idUsuario) throw new NaoEncontradoException("Endereço não encontrado.");
        if (await _enderecos.EmUsoEmPedidoAsync(idEndereco))
            throw new RegraDeNegocioException("Este endereço já foi usado em um pedido e fica guardado no histórico.");
        _enderecos.Remover(endereco);
        await _uow.SalvarAsync();
    }

    /// <summary>Cria a entidade a partir do formulário (usado aqui e no checkout).</summary>
    public static Endereco NovoEndereco(int idUsuario, EnderecoSalvarDto dto, bool forcarPrincipal) => new()
    {
        IdUsuario = idUsuario,
        Apelido = string.IsNullOrWhiteSpace(dto.Apelido) ? "Casa" : dto.Apelido.Trim(),
        Cep = dto.Cep.Trim(),
        Logradouro = dto.Logradouro.Trim(),
        Numero = dto.Numero.Trim(),
        Complemento = string.IsNullOrWhiteSpace(dto.Complemento) ? null : dto.Complemento.Trim(),
        Bairro = dto.Bairro.Trim(),
        Cidade = dto.Cidade.Trim(),
        Estado = dto.Estado.Trim().ToUpperInvariant(),
        Principal = dto.Principal || forcarPrincipal,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude
    };
}
