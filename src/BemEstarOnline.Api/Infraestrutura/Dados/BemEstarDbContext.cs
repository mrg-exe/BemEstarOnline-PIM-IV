using System.Text;
using BemEstarOnline.Api.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace BemEstarOnline.Api.Infraestrutura.Dados;

/// <summary>
/// Mapeamento objeto-relacional (Entity Framework Core).
/// As classes usam nomes em PascalCase (IdUsuario) e o banco usa snake_case (id_usuario);
/// a conversão é feita automaticamente no final do OnModelCreating.
/// </summary>
public class BemEstarDbContext : DbContext
{
    public BemEstarDbContext(DbContextOptions<BemEstarDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Endereco> Enderecos => Set<Endereco>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Estoque> Estoques => Set<Estoque>();
    public DbSet<AlertaEstoque> AlertasEstoque => Set<AlertaEstoque>();
    public DbSet<Cupom> Cupons => Set<Cupom>();
    public DbSet<Carrinho> Carrinhos => Set<Carrinho>();
    public DbSet<CarrinhoItem> CarrinhoItens => Set<CarrinhoItem>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
    public DbSet<PedidoHistoricoStatus> PedidoHistorico => Set<PedidoHistoricoStatus>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<DeliverySolicitacao> DeliverySolicitacoes => Set<DeliverySolicitacao>();
    public DbSet<Ouvidoria> Ouvidorias => Set<Ouvidoria>();
    public DbSet<RedefinicaoSenha> RedefinicoesSenha => Set<RedefinicaoSenha>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios").HasKey(x => x.IdUsuario);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Cpf).IsUnique();
            e.Ignore(x => x.NomeExibicao);
            e.Ignore(x => x.EhEquipe);
        });

        mb.Entity<Endereco>(e =>
        {
            e.ToTable("enderecos").HasKey(x => x.IdEndereco);
            e.Ignore(x => x.TextoCompleto);
            e.HasOne(x => x.Usuario).WithMany(x => x.Enderecos).HasForeignKey(x => x.IdUsuario);
        });

        mb.Entity<Categoria>(e =>
        {
            e.ToTable("categorias").HasKey(x => x.IdCategoria);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        mb.Entity<Medicamento>(e =>
        {
            e.ToTable("medicamentos").HasKey(x => x.IdMedicamento);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Ignore(x => x.QuantidadeEmEstoque);
            e.Ignore(x => x.Disponivel);
            e.HasOne(x => x.Categoria).WithMany(x => x.Medicamentos).HasForeignKey(x => x.IdCategoria);
        });

        mb.Entity<Estoque>(e =>
        {
            e.ToTable("estoques").HasKey(x => x.IdEstoque);
            e.HasIndex(x => x.IdMedicamento).IsUnique();
            e.Ignore(x => x.AbaixoDoMinimo);
            e.HasOne(x => x.Medicamento).WithOne(x => x.Estoque).HasForeignKey<Estoque>(x => x.IdMedicamento);
        });

        mb.Entity<AlertaEstoque>(e =>
        {
            e.ToTable("alertas_estoque").HasKey(x => x.IdAlerta);
            e.HasOne(x => x.Medicamento).WithMany().HasForeignKey(x => x.IdMedicamento);
        });

        mb.Entity<Cupom>(e =>
        {
            e.ToTable("cupons").HasKey(x => x.IdCupom);
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        mb.Entity<Carrinho>(e =>
        {
            e.ToTable("carrinhos").HasKey(x => x.IdCarrinho);
            e.HasIndex(x => x.IdUsuario).IsUnique();
            e.HasOne(x => x.Usuario).WithOne(x => x.Carrinho).HasForeignKey<Carrinho>(x => x.IdUsuario);
        });

        mb.Entity<CarrinhoItem>(e =>
        {
            e.ToTable("carrinho_itens").HasKey(x => x.IdItemCarrinho);
            e.HasOne(x => x.Carrinho).WithMany(x => x.Itens).HasForeignKey(x => x.IdCarrinho);
            e.HasOne(x => x.Medicamento).WithMany().HasForeignKey(x => x.IdMedicamento);
        });

        mb.Entity<Pedido>(e =>
        {
            e.ToTable("pedidos").HasKey(x => x.IdPedido);
            e.Ignore(x => x.ClientePodeCancelar);
            e.HasOne(x => x.Usuario).WithMany(x => x.Pedidos).HasForeignKey(x => x.IdUsuario);
            e.HasOne(x => x.Endereco).WithMany().HasForeignKey(x => x.IdEndereco);
            e.HasOne(x => x.Cupom).WithMany().HasForeignKey(x => x.IdCupom);
            e.HasOne(x => x.Entregador).WithMany().HasForeignKey(x => x.IdEntregador).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<PedidoItem>(e =>
        {
            e.ToTable("pedido_itens").HasKey(x => x.IdItemPedido);
            e.HasOne(x => x.Pedido).WithMany(x => x.Itens).HasForeignKey(x => x.IdPedido);
            e.HasOne(x => x.Medicamento).WithMany().HasForeignKey(x => x.IdMedicamento);
        });

        mb.Entity<PedidoHistoricoStatus>(e =>
        {
            e.ToTable("pedido_historico_status").HasKey(x => x.IdHistorico);
            e.HasOne(x => x.Pedido).WithMany(x => x.Historico).HasForeignKey(x => x.IdPedido);
        });

        mb.Entity<Pagamento>(e =>
        {
            e.ToTable("pagamentos").HasKey(x => x.IdPagamento);
            e.HasOne(x => x.Pedido).WithMany(x => x.Pagamentos).HasForeignKey(x => x.IdPedido);
        });

        mb.Entity<Assinatura>(e =>
        {
            e.ToTable("assinaturas").HasKey(x => x.IdAssinatura);
            e.Ignore(x => x.ValorPorEntrega);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            e.HasOne(x => x.Medicamento).WithMany().HasForeignKey(x => x.IdMedicamento);
        });

        mb.Entity<DeliverySolicitacao>(e =>
        {
            e.ToTable("delivery_solicitacoes").HasKey(x => x.IdDelivery);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<Ouvidoria>(e =>
        {
            e.ToTable("ouvidoria").HasKey(x => x.IdOuvidoria);
            e.HasIndex(x => x.Protocolo).IsUnique();
            e.Ignore(x => x.EhDiscriminacao);
            e.Ignore(x => x.EmAberto);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<RedefinicaoSenha>(e =>
        {
            e.ToTable("redefinicoes_senha").HasKey(x => x.IdRedefinicao);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Ignore(x => x.Valida);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        });

        // Convenção: toda coluna vira snake_case e todo decimal vira DECIMAL(10,2).
        foreach (var entidade in mb.Model.GetEntityTypes())
        {
            foreach (var propriedade in entidade.GetProperties())
            {
                propriedade.SetColumnName(ParaSnakeCase(propriedade.Name));
                if (propriedade.ClrType == typeof(decimal) || propriedade.ClrType == typeof(decimal?))
                    propriedade.SetPrecision(10);
                if (propriedade.ClrType == typeof(decimal) || propriedade.ClrType == typeof(decimal?))
                    propriedade.SetScale(2);
            }
        }
    }

    /// <summary>"IdItemPedido" → "id_item_pedido".</summary>
    public static string ParaSnakeCase(string nome)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
