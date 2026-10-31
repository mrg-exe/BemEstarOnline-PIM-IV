using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>Entregas programadas de medicamentos de uso contínuo, com 10% de desconto.</summary>
public class AssinaturaServico : IAssinaturaServico
{
    private readonly IAssinaturaRepositorio _assinaturas;
    private readonly IProdutoRepositorio _produtos;
    private readonly IUnidadeDeTrabalho _uow;

    public AssinaturaServico(IAssinaturaRepositorio assinaturas, IProdutoRepositorio produtos, IUnidadeDeTrabalho uow)
    {
        _assinaturas = assinaturas;
        _produtos = produtos;
        _uow = uow;
    }

    public async Task<AssinaturaDto> CriarAsync(int idUsuario, AssinaturaCriarDto dto)
    {
        var produto = await _produtos.ObterPorIdAsync(dto.IdMedicamento)
                      ?? throw new RegraDeNegocioException("Escolha um medicamento da lista.");
        if (!produto.Ativo) throw new RegraDeNegocioException("Este medicamento não está mais à venda.");
        if (!produto.UsoContinuo)
            throw new RegraDeNegocioException("A assinatura é só para medicamentos de uso contínuo.");
        if (!RegrasComerciais.PeriodicidadesAssinatura.Contains(dto.PeriodicidadeDias))
            throw new RegraDeNegocioException("Escolha entregas a cada 15, 30, 60 ou 90 dias.");

        var assinatura = new Assinatura
        {
            IdUsuario = idUsuario,
            IdMedicamento = produto.IdMedicamento,
            Medicamento = produto,
            NomePaciente = dto.NomePaciente.Trim(),
            Quantidade = dto.Quantidade,
            PeriodicidadeDias = dto.PeriodicidadeDias,
            ValorUnitario = produto.Preco,
            DescontoPercentual = RegrasComerciais.DescontoAssinaturaPercentual,
            ProximaEntrega = DateTime.Today.AddDays(1)
        };
        _assinaturas.Adicionar(assinatura);
        await _uow.SalvarAsync();
        return assinatura.ParaDto();
    }

    public async Task<List<AssinaturaDto>> ListarDoUsuarioAsync(int idUsuario) =>
        (await _assinaturas.ListarPorUsuarioAsync(idUsuario)).Select(a => a.ParaDto()).ToList();

    public async Task<List<AssinaturaDto>> ListarTodasAsync() =>
        (await _assinaturas.ListarTodasAsync()).Select(a => a.ParaDto()).ToList();

    public async Task<AssinaturaDto> AlterarStatusAsync(int idAssinatura, string status, int idUsuarioSolicitante, bool solicitanteEhEquipe)
    {
        var assinatura = await _assinaturas.ObterPorIdAsync(idAssinatura)
                         ?? throw new NaoEncontradoException("Assinatura não encontrada.");
        if (!solicitanteEhEquipe && assinatura.IdUsuario != idUsuarioSolicitante) throw new AcessoNegadoException();

        assinatura.AlterarStatus(status);
        await _uow.SalvarAsync();
        return assinatura.ParaDto();
    }
}
