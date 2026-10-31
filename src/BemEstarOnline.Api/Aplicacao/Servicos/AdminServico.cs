using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>Indicadores e listas da área administrativa da farmácia.</summary>
public class AdminServico : IAdminServico
{
    private readonly IRelatorioRepositorio _relatorios;
    private readonly IPedidoRepositorio _pedidos;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAssinaturaRepositorio _assinaturas;
    private readonly IDeliveryRepositorio _delivery;
    private readonly IAlertaEstoqueRepositorio _alertas;
    private readonly IAutomacoesBanco _automacoes;
    private readonly IUnidadeDeTrabalho _uow;

    public AdminServico(IRelatorioRepositorio relatorios, IPedidoRepositorio pedidos, IUsuarioRepositorio usuarios,
                        IAssinaturaRepositorio assinaturas, IDeliveryRepositorio delivery, IAlertaEstoqueRepositorio alertas,
                        IAutomacoesBanco automacoes, IUnidadeDeTrabalho uow)
    {
        _relatorios = relatorios;
        _pedidos = pedidos;
        _usuarios = usuarios;
        _assinaturas = assinaturas;
        _delivery = delivery;
        _alertas = alertas;
        _automacoes = automacoes;
        _uow = uow;
    }

    public async Task<PainelDto> PainelAsync()
    {
        var fim = DateTime.Today;
        var inicio = fim.AddDays(-13);

        var vendas = await _relatorios.VendasPorDiaAsync(inicio, fim);
        // Preenche os dias sem venda com zero para o gráfico ficar contínuo.
        var porDia = Enumerable.Range(0, 14)
            .Select(i => inicio.AddDays(i))
            .Select(d => vendas.FirstOrDefault(x => x.Dia.Date == d) is { } v
                ? new VendaDiaDto(d, v.QuantidadePedidos, v.Faturamento)
                : new VendaDiaDto(d, 0, 0))
            .ToList();

        var (pedidosHoje, vendasHoje, vendasMes, emAberto) = await _pedidos.IndicadoresAsync();
        var clientes = (await _usuarios.ListarAsync()).Count(u => u.Perfil == Perfis.Cliente);

        return new PainelDto(
            vendasHoje, vendasMes, pedidosHoje, emAberto, clientes,
            await _assinaturas.ContarAtivasAsync(),
            await _delivery.ContarAbertasAsync(),
            porDia,
            (await _relatorios.EstoqueBaixoAsync()).Select(e => new EstoqueBaixoDto(e.IdMedicamento, e.Nome, e.Quantidade, e.QuantidadeMinima)).ToList(),
            (await _relatorios.VencendoAsync(60)).Select(v => new VencendoDto(v.IdMedicamento, v.Nome, v.ValidadeLote, v.DiasRestantes)).ToList(),
            _automacoes.ExecutadasPeloBanco ? "MySQL (triggers e procedures ativos)" : "Memória (modo demonstração)");
    }

    public async Task<List<ClienteDto>> ClientesAsync()
    {
        var usuarios = await _usuarios.ListarAsync();
        var resumo = (await _pedidos.ResumoPorClienteAsync()).ToDictionary(r => r.idUsuario);
        return usuarios
            .Select(u => resumo.TryGetValue(u.IdUsuario, out var r)
                ? new ClienteDto(u.IdUsuario, u.NomeExibicao, u.Email, u.Telefone, u.Perfil, u.Ativo, r.quantidade, r.total, u.CriadoEm)
                : new ClienteDto(u.IdUsuario, u.NomeExibicao, u.Email, u.Telefone, u.Perfil, u.Ativo, 0, 0, u.CriadoEm))
            .OrderByDescending(c => c.TotalGasto)
            .ToList();
    }

    public async Task<List<AlertaDto>> AlertasAsync()
    {
        var alertas = await _alertas.ListarPendentesAsync();
        return alertas.Select(a => new AlertaDto(a.IdAlerta, a.IdMedicamento, a.Medicamento?.Nome ?? "", a.QuantidadeAtual,
                                                 a.QuantidadeMinima, a.Medicamento?.QuantidadeEmEstoque ?? 0, a.CriadoEm))
                      .ToList();
    }

    public async Task ResolverAlertaAsync(int idAlerta)
    {
        var alerta = await _alertas.ObterPorIdAsync(idAlerta) ?? throw new NaoEncontradoException("Alerta não encontrado.");
        alerta.Resolvido = true;
        alerta.ResolvidoEm = DateTime.Now;
        await _uow.SalvarAsync();
    }
}
