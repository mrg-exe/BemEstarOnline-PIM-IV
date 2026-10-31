using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>
/// Ouvidoria e canal de denúncia (RF30): qualquer pessoa registra (com ou sem conta, anônima ou não),
/// acompanha pelo protocolo, e a equipe responde pelo painel. Denúncias de discriminação vêm primeiro.
/// </summary>
public class OuvidoriaServico : IOuvidoriaServico
{
    private readonly IOuvidoriaRepositorio _ouvidoria;
    private readonly IUnidadeDeTrabalho _uow;

    public OuvidoriaServico(IOuvidoriaRepositorio ouvidoria, IUnidadeDeTrabalho uow)
    {
        _ouvidoria = ouvidoria;
        _uow = uow;
    }

    public async Task<OuvidoriaProtocoloDto> RegistrarAsync(int? idUsuario, OuvidoriaCriarDto dto)
    {
        var manifestacao = Ouvidoria.Registrar(dto.Tipo, dto.SubtipoDiscriminacao, dto.Descricao, dto.Anonimo, idUsuario, dto.Contato);
        // O sorteio quase nunca repete, mas conferimos para o protocolo ser único.
        while (await _ouvidoria.ProtocoloExisteAsync(manifestacao.Protocolo))
            manifestacao.Protocolo = Ouvidoria.GerarProtocolo();
        _ouvidoria.Adicionar(manifestacao);
        await _uow.SalvarAsync();
        return manifestacao.ParaProtocoloDto();
    }

    public async Task<OuvidoriaProtocoloDto> ConsultarAsync(string protocolo)
    {
        var manifestacao = await _ouvidoria.ObterPorProtocoloAsync((protocolo ?? "").Trim().ToUpperInvariant())
                           ?? throw new NaoEncontradoException("Protocolo não encontrado. Confira as letras e os números.");
        return manifestacao.ParaProtocoloDto();
    }

    /// <summary>Em aberto primeiro; dentro delas, denúncias de discriminação primeiro; depois as mais novas.</summary>
    public async Task<List<OuvidoriaDto>> ListarAsync() =>
        (await _ouvidoria.ListarAsync())
            .OrderByDescending(o => o.EmAberto)
            .ThenByDescending(o => o.EhDiscriminacao)
            .ThenByDescending(o => o.CriadoEm)
            .Select(o => o.ParaDto())
            .ToList();

    public async Task<OuvidoriaDto> AtualizarAsync(int idOuvidoria, OuvidoriaAtualizarDto dto)
    {
        var manifestacao = await _ouvidoria.ObterPorIdAsync(idOuvidoria)
                           ?? throw new NaoEncontradoException("Manifestação não encontrada.");
        manifestacao.Atualizar(dto.Status, dto.Resposta);
        await _uow.SalvarAsync();
        return manifestacao.ParaDto();
    }
}
