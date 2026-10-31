using System.Data;
using System.Data.Common;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace BemEstarOnline.Api.Infraestrutura.Automacoes;

/// <summary>Relatórios no MySQL: chama as STORED PROCEDURES (database/03_procedures.sql).</summary>
public class RelatorioRepositorioMySql : IRelatorioRepositorio
{
    private readonly BemEstarDbContext _db;
    public RelatorioRepositorioMySql(BemEstarDbContext db) => _db = db;

    public Task<List<VendaDia>> VendasPorDiaAsync(DateTime inicio, DateTime fim) =>
        ExecutarAsync("sp_relatorio_vendas", leitor => new VendaDia(
                leitor.GetDateTime(0), Convert.ToInt32(leitor.GetValue(1)), Convert.ToDecimal(leitor.GetValue(2))),
            ("p_inicio", inicio.Date), ("p_fim", fim.Date));

    public Task<List<ProdutoEstoqueBaixo>> EstoqueBaixoAsync() =>
        ExecutarAsync("sp_produtos_estoque_baixo", leitor => new ProdutoEstoqueBaixo(
            Convert.ToInt32(leitor.GetValue(0)), leitor.GetString(1),
            Convert.ToInt32(leitor.GetValue(2)), Convert.ToInt32(leitor.GetValue(3))));

    public Task<List<ProdutoVencendo>> VencendoAsync(int dias) =>
        ExecutarAsync("sp_produtos_vencendo", leitor => new ProdutoVencendo(
                Convert.ToInt32(leitor.GetValue(0)), leitor.GetString(1),
                leitor.GetDateTime(2), Convert.ToInt32(leitor.GetValue(3))),
            ("p_dias", dias));

    private async Task<List<T>> ExecutarAsync<T>(string procedure, Func<DbDataReader, T> ler,
                                                 params (string nome, object valor)[] parametros)
    {
        var conexao = _db.Database.GetDbConnection();
        var abriuAqui = conexao.State != ConnectionState.Open;
        if (abriuAqui) await conexao.OpenAsync();
        try
        {
            await using var comando = conexao.CreateCommand();
            comando.CommandText = procedure;
            comando.CommandType = CommandType.StoredProcedure;
            foreach (var (nome, valor) in parametros)
            {
                var p = comando.CreateParameter();
                p.ParameterName = nome;
                p.Value = valor;
                comando.Parameters.Add(p);
            }

            var resultado = new List<T>();
            await using var leitor = await comando.ExecuteReaderAsync();
            while (await leitor.ReadAsync()) resultado.Add(ler(leitor));
            return resultado;
        }
        finally
        {
            if (abriuAqui) await conexao.CloseAsync();
        }
    }
}

/// <summary>Relatórios no modo demonstração: mesmo cálculo das procedures, feito em C#.</summary>
public class RelatorioRepositorioEmMemoria : IRelatorioRepositorio
{
    private readonly BemEstarDbContext _db;
    public RelatorioRepositorioEmMemoria(BemEstarDbContext db) => _db = db;

    public async Task<List<VendaDia>> VendasPorDiaAsync(DateTime inicio, DateTime fim)
    {
        var limite = fim.Date.AddDays(1);
        var pedidos = await _db.Pedidos.AsNoTracking()
            .Where(p => p.CriadoEm >= inicio.Date && p.CriadoEm < limite && p.Status != StatusPedido.Cancelado)
            .ToListAsync();
        return pedidos.GroupBy(p => p.CriadoEm.Date)
                      .Select(g => new VendaDia(g.Key, g.Count(), g.Sum(p => p.Total)))
                      .OrderBy(v => v.Dia).ToList();
    }

    public async Task<List<ProdutoEstoqueBaixo>> EstoqueBaixoAsync() =>
        (await _db.Estoques.AsNoTracking().Include(e => e.Medicamento)
            .Where(e => e.Quantidade <= e.QuantidadeMinima && e.Medicamento!.Ativo).ToListAsync())
        .OrderBy(e => e.Quantidade)
        .Select(e => new ProdutoEstoqueBaixo(e.IdMedicamento, e.Medicamento!.Nome, e.Quantidade, e.QuantidadeMinima))
        .ToList();

    public async Task<List<ProdutoVencendo>> VencendoAsync(int dias)
    {
        var hoje = DateTime.Today;
        var limite = hoje.AddDays(dias);
        var produtos = await _db.Medicamentos.AsNoTracking()
            .Where(m => m.Ativo && m.ValidadeLote != null && m.ValidadeLote <= limite)
            .ToListAsync();
        return produtos.OrderBy(m => m.ValidadeLote)
                       .Select(m => new ProdutoVencendo(m.IdMedicamento, m.Nome, m.ValidadeLote!.Value,
                                                        (int)(m.ValidadeLote!.Value.Date - hoje).TotalDays))
                       .ToList();
    }
}
