using BemEstarOnline.Api.Dominio.Entidades;

namespace BemEstarOnline.Api.Dominio.Interfaces;

/*
 * Contratos de acesso a dados (padrão Repository).
 * A camada de Aplicação conhece apenas estas interfaces; quem decide se os dados
 * vêm do MySQL ou da memória é a camada de Infraestrutura (inversão de dependência - SOLID "D").
 */

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterPorIdAsync(int id);
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<bool> EmailExisteAsync(string email);
    Task<bool> CpfExisteAsync(string cpf);
    Task<bool> ExisteAlgumAsync();
    void Adicionar(Usuario usuario);
    Task<List<Usuario>> ListarAsync();
}

public interface IEnderecoRepositorio
{
    Task<List<Endereco>> ListarPorUsuarioAsync(int idUsuario);
    Task<Endereco?> ObterPorIdAsync(int idEndereco);
    void Adicionar(Endereco endereco);
    void Remover(Endereco endereco);
    Task<bool> EmUsoEmPedidoAsync(int idEndereco);
}

public interface ICategoriaRepositorio
{
    Task<List<Categoria>> ListarAsync();
    Task<Categoria?> ObterPorIdAsync(int id);
    Task<bool> ExisteAlgumaAsync();
    void Adicionar(Categoria categoria);
}

public record FiltroProdutos(string? Categoria = null, string? Busca = null, bool? FarmaciaPopular = null,
                             bool? UsoContinuo = null, bool IncluirInativos = false);

public interface IProdutoRepositorio
{
    Task<List<Medicamento>> ListarAsync(FiltroProdutos filtro);
    Task<Medicamento?> ObterPorIdAsync(int id);
    Task<Medicamento?> ObterPorSlugAsync(string slug);
    Task<List<Medicamento>> ObterVariosAsync(IEnumerable<int> ids);
    Task<bool> SlugExisteAsync(string slug, int? ignorarId = null);
    Task<bool> ExisteAlgumAsync();
    void Adicionar(Medicamento medicamento);
}

public interface ICarrinhoRepositorio
{
    Task<Carrinho?> ObterPorUsuarioAsync(int idUsuario);
    void Adicionar(Carrinho carrinho);
    void RemoverItens(IEnumerable<CarrinhoItem> itens);
}

public interface ICupomRepositorio
{
    Task<Cupom?> ObterPorCodigoAsync(string codigo);
    Task<bool> ExisteAlgumAsync();
    void Adicionar(Cupom cupom);
}

public interface IPedidoRepositorio
{
    Task<Pedido?> ObterDetalheAsync(int idPedido);
    Task<List<Pedido>> ListarPorUsuarioAsync(int idUsuario);
    Task<List<Pedido>> ListarAsync(string? status, int limite);
    Task<bool> ExisteAlgumAsync();
    void Adicionar(Pedido pedido);
    /// <summary>Pedidos em separação sem entregador + entregas em andamento deste entregador.</summary>
    Task<List<Pedido>> ListarParaEntregadorAsync(int idEntregador);
    Task<List<(int idUsuario, int quantidade, decimal total)>> ResumoPorClienteAsync();
    Task<(int pedidosHoje, decimal vendasHoje, decimal vendasMes, int emAberto)> IndicadoresAsync();
}

public interface IAssinaturaRepositorio
{
    Task<List<Assinatura>> ListarPorUsuarioAsync(int idUsuario);
    Task<List<Assinatura>> ListarParaAlterarAsync(int idUsuario);
    Task<List<Assinatura>> ListarTodasAsync();
    Task<Assinatura?> ObterPorIdAsync(int id);
    Task<int> ContarAtivasAsync();
    void Adicionar(Assinatura assinatura);
}

public interface IDeliveryRepositorio
{
    Task<List<DeliverySolicitacao>> ListarPorUsuarioAsync(int idUsuario);
    /// <summary>Mesma lista, mas para alterar (usada ao excluir a conta).</summary>
    Task<List<DeliverySolicitacao>> ListarParaAlterarAsync(int idUsuario);
    Task<List<DeliverySolicitacao>> ListarTodasAsync();
    Task<DeliverySolicitacao?> ObterPorIdAsync(int id);
    Task<int> ContarAbertasAsync();
    void Adicionar(DeliverySolicitacao solicitacao);
}

public interface IOuvidoriaRepositorio
{
    Task<Ouvidoria?> ObterPorProtocoloAsync(string protocolo);
    Task<Ouvidoria?> ObterPorIdAsync(int id);
    Task<bool> ProtocoloExisteAsync(string protocolo);
    Task<List<Ouvidoria>> ListarAsync();
    void Adicionar(Ouvidoria ouvidoria);
}

public interface IRedefinicaoSenhaRepositorio
{
    Task<RedefinicaoSenha?> ObterPorHashAsync(string tokenHash);
    Task<List<RedefinicaoSenha>> ListarDesdeAsync(int idUsuario, DateTime desde);
    void Adicionar(RedefinicaoSenha pedido);
}

public interface IAlertaEstoqueRepositorio
{
    Task<List<AlertaEstoque>> ListarPendentesAsync();
    Task<List<AlertaEstoque>> ListarPendentesDoProdutoAsync(int idMedicamento);
    Task<AlertaEstoque?> ObterPorIdAsync(int id);
}

/// <summary>Grava de uma vez todas as alterações feitas pelos repositórios (Unit of Work).</summary>
public interface IUnidadeDeTrabalho
{
    Task SalvarAsync();
}
