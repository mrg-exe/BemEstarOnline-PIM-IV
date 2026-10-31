using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace BemEstarOnline.Api.Infraestrutura.Automacoes;

/// <summary>
/// Modo MySQL: não faz nada, porque os TRIGGERS do banco já executam as regras
/// (ver database/02_triggers.sql). Fazer de novo aqui duplicaria a baixa de estoque.
/// </summary>
public class AutomacoesViaTriggersMySql : IAutomacoesBanco
{
    public bool ExecutadasPeloBanco => true;
    public Task AposCriarPedidoAsync(Pedido pedido) => Task.CompletedTask;
    public Task AposAlterarStatusAsync(Pedido pedido, string statusAnterior) => Task.CompletedTask;
}

/// <summary>
/// Modo demonstração (banco em memória): reproduz em C# exatamente o que os triggers fazem no MySQL.
/// </summary>
public class AutomacoesEmMemoria : IAutomacoesBanco
{
    private readonly BemEstarDbContext _db;
    public AutomacoesEmMemoria(BemEstarDbContext db) => _db = db;

    public bool ExecutadasPeloBanco => false;

    // Equivale a trg_pedidos_ai_historico + trg_pedido_itens_ai_baixa_estoque
    public async Task AposCriarPedidoAsync(Pedido pedido)
    {
        _db.PedidoHistorico.Add(new PedidoHistoricoStatus
        {
            IdPedido = pedido.IdPedido, StatusAnterior = null, StatusNovo = pedido.Status, AlteradoEm = pedido.CriadoEm
        });

        foreach (var item in pedido.Itens)
        {
            var estoque = await _db.Estoques.FirstOrDefaultAsync(e => e.IdMedicamento == item.IdMedicamento);
            if (estoque is null) continue;

            estoque.Quantidade = Math.Max(0, estoque.Quantidade - item.Quantidade);
            estoque.AtualizadoEm = DateTime.Now;

            if (estoque.AbaixoDoMinimo)
            {
                var jaTemAlerta = await _db.AlertasEstoque.AnyAsync(a => a.IdMedicamento == item.IdMedicamento && !a.Resolvido)
                                  || _db.AlertasEstoque.Local.Any(a => a.IdMedicamento == item.IdMedicamento && !a.Resolvido);
                if (!jaTemAlerta)
                    _db.AlertasEstoque.Add(new AlertaEstoque
                    {
                        IdMedicamento = item.IdMedicamento,
                        QuantidadeAtual = estoque.Quantidade,
                        QuantidadeMinima = estoque.QuantidadeMinima
                    });
            }
        }
        await _db.SaveChangesAsync();
    }

    // Equivale a trg_pedidos_au_status
    public async Task AposAlterarStatusAsync(Pedido pedido, string statusAnterior)
    {
        if (statusAnterior == pedido.Status) return;

        _db.PedidoHistorico.Add(new PedidoHistoricoStatus
        {
            IdPedido = pedido.IdPedido, StatusAnterior = statusAnterior, StatusNovo = pedido.Status, AlteradoEm = DateTime.Now
        });

        if (pedido.Status == StatusPedido.Cancelado)
        {
            foreach (var item in pedido.Itens)
            {
                var estoque = await _db.Estoques.FirstOrDefaultAsync(e => e.IdMedicamento == item.IdMedicamento);
                if (estoque is null) continue;
                estoque.Quantidade += item.Quantidade;
                estoque.AtualizadoEm = DateTime.Now;
            }
        }
        await _db.SaveChangesAsync();
    }
}
