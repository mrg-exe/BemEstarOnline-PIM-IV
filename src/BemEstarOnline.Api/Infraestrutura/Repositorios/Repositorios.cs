using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace BemEstarOnline.Api.Infraestrutura.Repositorios;

/*
 * Implementações dos repositórios com Entity Framework Core.
 * Servem tanto para o MySQL quanto para o banco em memória: o que muda é só a configuração no Program.cs.
 */

public class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly BemEstarDbContext _db;
    public UnidadeDeTrabalho(BemEstarDbContext db) => _db = db;
    public Task SalvarAsync() => _db.SaveChangesAsync();
}

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly BemEstarDbContext _db;
    public UsuarioRepositorio(BemEstarDbContext db) => _db = db;

    public Task<Usuario?> ObterPorIdAsync(int id) => _db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id);
    public Task<Usuario?> ObterPorEmailAsync(string email) => _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
    public Task<bool> EmailExisteAsync(string email) => _db.Usuarios.AnyAsync(u => u.Email == email);
    public Task<bool> CpfExisteAsync(string cpf) => _db.Usuarios.AnyAsync(u => u.Cpf == cpf);
    public Task<bool> ExisteAlgumAsync() => _db.Usuarios.AnyAsync();
    public void Adicionar(Usuario usuario) => _db.Usuarios.Add(usuario);
    public Task<List<Usuario>> ListarAsync() => _db.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();
}

public class EnderecoRepositorio : IEnderecoRepositorio
{
    private readonly BemEstarDbContext _db;
    public EnderecoRepositorio(BemEstarDbContext db) => _db = db;

    public Task<List<Endereco>> ListarPorUsuarioAsync(int idUsuario) =>
        _db.Enderecos.Where(e => e.IdUsuario == idUsuario).OrderByDescending(e => e.Principal).ThenBy(e => e.IdEndereco).ToListAsync();
    public Task<Endereco?> ObterPorIdAsync(int idEndereco) => _db.Enderecos.FirstOrDefaultAsync(e => e.IdEndereco == idEndereco);
    public void Adicionar(Endereco endereco) => _db.Enderecos.Add(endereco);
    public void Remover(Endereco endereco) => _db.Enderecos.Remove(endereco);
    public Task<bool> EmUsoEmPedidoAsync(int idEndereco) => _db.Pedidos.AnyAsync(p => p.IdEndereco == idEndereco);
}

public class CategoriaRepositorio : ICategoriaRepositorio
{
    private readonly BemEstarDbContext _db;
    public CategoriaRepositorio(BemEstarDbContext db) => _db = db;

    public Task<List<Categoria>> ListarAsync() =>
        _db.Categorias.AsNoTracking().Include(c => c.Medicamentos).Where(c => c.Ativo).OrderBy(c => c.Ordem).ThenBy(c => c.Nome).ToListAsync();
    public Task<Categoria?> ObterPorIdAsync(int id) => _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == id);
    public Task<bool> ExisteAlgumaAsync() => _db.Categorias.AnyAsync();
    public void Adicionar(Categoria categoria) => _db.Categorias.Add(categoria);
}

public class ProdutoRepositorio : IProdutoRepositorio
{
    private readonly BemEstarDbContext _db;
    public ProdutoRepositorio(BemEstarDbContext db) => _db = db;

    private IQueryable<Medicamento> Base() => _db.Medicamentos.Include(m => m.Categoria).Include(m => m.Estoque);

    public async Task<List<Medicamento>> ListarAsync(FiltroProdutos filtro)
    {
        var consulta = Base().AsNoTracking();
        if (!filtro.IncluirInativos) consulta = consulta.Where(m => m.Ativo);
        if (!string.IsNullOrWhiteSpace(filtro.Categoria)) consulta = consulta.Where(m => m.Categoria!.Slug == filtro.Categoria);
        if (filtro.FarmaciaPopular == true) consulta = consulta.Where(m => m.FarmaciaPopular);
        if (filtro.UsoContinuo == true) consulta = consulta.Where(m => m.UsoContinuo);

        var lista = await consulta.OrderBy(m => m.Nome).ToListAsync();

        // A busca é feita em memória para ignorar acentos ("dipirona" acha "Dipirona Sódica").
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = Texto.Normalizar(filtro.Busca);
            lista = lista.Where(m => Texto.Normalizar($"{m.Nome} {m.PrincipioAtivo} {m.Fabricante} {m.Descricao} {m.Tipo}").Contains(termo))
                         .OrderByDescending(m => Texto.Normalizar(m.Nome).StartsWith(termo))
                         .ThenBy(m => m.Nome)
                         .ToList();
        }
        return lista;
    }

    public Task<Medicamento?> ObterPorIdAsync(int id) => Base().FirstOrDefaultAsync(m => m.IdMedicamento == id);
    public Task<Medicamento?> ObterPorSlugAsync(string slug) => Base().FirstOrDefaultAsync(m => m.Slug == slug);
    public async Task<List<Medicamento>> ObterVariosAsync(IEnumerable<int> ids)
    {
        // O catálogo da farmácia é pequeno (dezenas/centenas de itens), então carregamos e filtramos em memória.
        // Isso evita diferenças de tradução de "IN (lista)" entre o MySQL e o banco em memória.
        var conjunto = ids.ToHashSet();
        var todos = await Base().ToListAsync();
        return todos.Where(m => conjunto.Contains(m.IdMedicamento)).ToList();
    }
    public Task<bool> SlugExisteAsync(string slug, int? ignorarId = null) =>
        _db.Medicamentos.AnyAsync(m => m.Slug == slug && (ignorarId == null || m.IdMedicamento != ignorarId));
    public Task<bool> ExisteAlgumAsync() => _db.Medicamentos.AnyAsync();
    public void Adicionar(Medicamento medicamento) => _db.Medicamentos.Add(medicamento);
}

public class CarrinhoRepositorio : ICarrinhoRepositorio
{
    private readonly BemEstarDbContext _db;
    public CarrinhoRepositorio(BemEstarDbContext db) => _db = db;

    public Task<Carrinho?> ObterPorUsuarioAsync(int idUsuario) =>
        _db.Carrinhos.Include(c => c.Itens).ThenInclude(i => i.Medicamento).ThenInclude(m => m!.Estoque)
                     .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);
    public void Adicionar(Carrinho carrinho) => _db.Carrinhos.Add(carrinho);
    public void RemoverItens(IEnumerable<CarrinhoItem> itens) => _db.CarrinhoItens.RemoveRange(itens);
}

public class CupomRepositorio : ICupomRepositorio
{
    private readonly BemEstarDbContext _db;
    public CupomRepositorio(BemEstarDbContext db) => _db = db;

    public Task<Cupom?> ObterPorCodigoAsync(string codigo) => _db.Cupons.FirstOrDefaultAsync(c => c.Codigo == codigo);
    public Task<bool> ExisteAlgumAsync() => _db.Cupons.AnyAsync();
    public void Adicionar(Cupom cupom) => _db.Cupons.Add(cupom);
}

public class PedidoRepositorio : IPedidoRepositorio
{
    private readonly BemEstarDbContext _db;
    public PedidoRepositorio(BemEstarDbContext db) => _db = db;

    public Task<Pedido?> ObterDetalheAsync(int idPedido) =>
        _db.Pedidos
           .Include(p => p.Usuario)
           .Include(p => p.Entregador)
           .Include(p => p.Endereco)
           .Include(p => p.Itens)
           .Include(p => p.Pagamentos)
           .Include(p => p.Historico)
           .FirstOrDefaultAsync(p => p.IdPedido == idPedido);

    public Task<List<Pedido>> ListarPorUsuarioAsync(int idUsuario) =>
        _db.Pedidos.AsNoTracking().Include(p => p.Itens).Where(p => p.IdUsuario == idUsuario)
           .OrderByDescending(p => p.CriadoEm).ToListAsync();

    public Task<List<Pedido>> ListarAsync(string? status, int limite)
    {
        var consulta = _db.Pedidos.AsNoTracking().Include(p => p.Itens).Include(p => p.Usuario).AsQueryable();
        if (status == "ABERTOS") consulta = consulta.Where(p => p.Status != StatusPedido.Entregue && p.Status != StatusPedido.Cancelado);
        else if (!string.IsNullOrWhiteSpace(status)) consulta = consulta.Where(p => p.Status == status);
        return consulta.OrderByDescending(p => p.CriadoEm).Take(limite).ToListAsync();
    }

    public Task<bool> ExisteAlgumAsync() => _db.Pedidos.AnyAsync();
    public void Adicionar(Pedido pedido) => _db.Pedidos.Add(pedido);

    public Task<List<Pedido>> ListarParaEntregadorAsync(int idEntregador) =>
        _db.Pedidos.AsNoTracking()
           .Include(p => p.Usuario).Include(p => p.Endereco).Include(p => p.Itens).Include(p => p.Pagamentos)
           .Where(p => (p.Status == StatusPedido.EmSeparacao && p.IdEntregador == null) ||
                       (p.IdEntregador == idEntregador && (p.Status == StatusPedido.EmSeparacao || p.Status == StatusPedido.SaiuParaEntrega)))
           .OrderBy(p => p.CriadoEm)
           .ToListAsync();

    public async Task<List<(int idUsuario, int quantidade, decimal total)>> ResumoPorClienteAsync()
    {
        var dados = await _db.Pedidos.AsNoTracking()
            .Where(p => p.Status != StatusPedido.Cancelado)
            .GroupBy(p => p.IdUsuario)
            .Select(g => new { IdUsuario = g.Key, Quantidade = g.Count(), Total = g.Sum(p => p.Total) })
            .ToListAsync();
        return dados.Select(d => (d.IdUsuario, d.Quantidade, d.Total)).ToList();
    }

    public async Task<(int pedidosHoje, decimal vendasHoje, decimal vendasMes, int emAberto)> IndicadoresAsync()
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var doMes = await _db.Pedidos.AsNoTracking()
            .Where(p => p.CriadoEm >= inicioMes && p.Status != StatusPedido.Cancelado)
            .Select(p => new { p.CriadoEm, p.Total })
            .ToListAsync();
        var emAberto = await _db.Pedidos.CountAsync(p => p.Status != StatusPedido.Entregue && p.Status != StatusPedido.Cancelado);

        var deHoje = doMes.Where(p => p.CriadoEm >= hoje).ToList();
        return (deHoje.Count, deHoje.Sum(p => p.Total), doMes.Sum(p => p.Total), emAberto);
    }
}

public class AssinaturaRepositorio : IAssinaturaRepositorio
{
    private readonly BemEstarDbContext _db;
    public AssinaturaRepositorio(BemEstarDbContext db) => _db = db;

    public Task<List<Assinatura>> ListarPorUsuarioAsync(int idUsuario) =>
        _db.Assinaturas.AsNoTracking().Include(a => a.Medicamento).Where(a => a.IdUsuario == idUsuario)
           .OrderByDescending(a => a.CriadaEm).ToListAsync();
    public Task<List<Assinatura>> ListarParaAlterarAsync(int idUsuario) =>
        _db.Assinaturas.Where(a => a.IdUsuario == idUsuario).ToListAsync();
    public Task<List<Assinatura>> ListarTodasAsync() =>
        _db.Assinaturas.AsNoTracking().Include(a => a.Medicamento).Include(a => a.Usuario)
           .OrderBy(a => a.Status).ThenBy(a => a.ProximaEntrega).ToListAsync();
    public Task<Assinatura?> ObterPorIdAsync(int id) =>
        _db.Assinaturas.Include(a => a.Medicamento).Include(a => a.Usuario).FirstOrDefaultAsync(a => a.IdAssinatura == id);
    public Task<int> ContarAtivasAsync() => _db.Assinaturas.CountAsync(a => a.Status == StatusAssinatura.Ativa);
    public void Adicionar(Assinatura assinatura) => _db.Assinaturas.Add(assinatura);
}

public class DeliveryRepositorio : IDeliveryRepositorio
{
    private readonly BemEstarDbContext _db;
    public DeliveryRepositorio(BemEstarDbContext db) => _db = db;

    public Task<List<DeliverySolicitacao>> ListarPorUsuarioAsync(int idUsuario) =>
        _db.DeliverySolicitacoes.AsNoTracking().Where(d => d.IdUsuario == idUsuario).OrderByDescending(d => d.CriadoEm).ToListAsync();
    public Task<List<DeliverySolicitacao>> ListarParaAlterarAsync(int idUsuario) =>
        _db.DeliverySolicitacoes.Where(d => d.IdUsuario == idUsuario).ToListAsync();
    public Task<List<DeliverySolicitacao>> ListarTodasAsync() =>
        _db.DeliverySolicitacoes.AsNoTracking().OrderByDescending(d => d.CriadoEm).Take(200).ToListAsync();
    public Task<DeliverySolicitacao?> ObterPorIdAsync(int id) => _db.DeliverySolicitacoes.FirstOrDefaultAsync(d => d.IdDelivery == id);
    public Task<int> ContarAbertasAsync() =>
        _db.DeliverySolicitacoes.CountAsync(d => d.Status != StatusDelivery.Concluido && d.Status != StatusDelivery.Cancelado);
    public void Adicionar(DeliverySolicitacao solicitacao) => _db.DeliverySolicitacoes.Add(solicitacao);
}

public class OuvidoriaRepositorio : IOuvidoriaRepositorio
{
    private readonly BemEstarDbContext _db;
    public OuvidoriaRepositorio(BemEstarDbContext db) => _db = db;

    public Task<Ouvidoria?> ObterPorProtocoloAsync(string protocolo) =>
        _db.Ouvidorias.AsNoTracking().FirstOrDefaultAsync(o => o.Protocolo == protocolo);
    public Task<Ouvidoria?> ObterPorIdAsync(int id) =>
        _db.Ouvidorias.Include(o => o.Usuario).FirstOrDefaultAsync(o => o.IdOuvidoria == id);
    public Task<bool> ProtocoloExisteAsync(string protocolo) => _db.Ouvidorias.AnyAsync(o => o.Protocolo == protocolo);
    public Task<List<Ouvidoria>> ListarAsync() =>
        _db.Ouvidorias.AsNoTracking().Include(o => o.Usuario).OrderByDescending(o => o.CriadoEm).Take(300).ToListAsync();
    public void Adicionar(Ouvidoria ouvidoria) => _db.Ouvidorias.Add(ouvidoria);
}

public class RedefinicaoSenhaRepositorio : IRedefinicaoSenhaRepositorio
{
    private readonly BemEstarDbContext _db;
    public RedefinicaoSenhaRepositorio(BemEstarDbContext db) => _db = db;

    public Task<RedefinicaoSenha?> ObterPorHashAsync(string tokenHash) =>
        _db.RedefinicoesSenha.Include(r => r.Usuario).FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
    public Task<List<RedefinicaoSenha>> ListarDesdeAsync(int idUsuario, DateTime desde) =>
        _db.RedefinicoesSenha.Where(r => r.IdUsuario == idUsuario && r.CriadoEm >= desde).ToListAsync();
    public void Adicionar(RedefinicaoSenha pedido) => _db.RedefinicoesSenha.Add(pedido);
}

public class AlertaEstoqueRepositorio : IAlertaEstoqueRepositorio
{
    private readonly BemEstarDbContext _db;
    public AlertaEstoqueRepositorio(BemEstarDbContext db) => _db = db;

    public Task<List<AlertaEstoque>> ListarPendentesAsync() =>
        _db.AlertasEstoque.Include(a => a.Medicamento).ThenInclude(m => m!.Estoque)
           .Where(a => !a.Resolvido).OrderByDescending(a => a.CriadoEm).ToListAsync();
    public Task<List<AlertaEstoque>> ListarPendentesDoProdutoAsync(int idMedicamento) =>
        _db.AlertasEstoque.Where(a => !a.Resolvido && a.IdMedicamento == idMedicamento).ToListAsync();
    public Task<AlertaEstoque?> ObterPorIdAsync(int id) => _db.AlertasEstoque.FirstOrDefaultAsync(a => a.IdAlerta == id);
}

/// <summary>Utilitário de texto para buscas sem acento e sem diferenciar maiúsculas.</summary>
public static class Texto
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var decomposto = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in decomposto)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }
}
