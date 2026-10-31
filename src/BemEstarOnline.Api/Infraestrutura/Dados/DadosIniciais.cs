using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BemEstarOnline.Api.Infraestrutura.Dados;

/// <summary>
/// Preenche o banco na primeira execução: catálogo (se ainda não veio do script SQL),
/// usuários de teste, endereços, pedidos dos últimos 14 dias, assinaturas e solicitações.
/// Cada etapa só roda se a tabela correspondente estiver vazia, então é seguro reiniciar a API.
/// </summary>
public class DadosIniciais
{
    private readonly BemEstarDbContext _db;
    private readonly ISenhaHasher _hasher;
    private readonly IAutomacoesBanco _automacoes;
    private readonly ILogger<DadosIniciais> _log;

    public DadosIniciais(BemEstarDbContext db, ISenhaHasher hasher, IAutomacoesBanco automacoes, ILogger<DadosIniciais> log)
    {
        _db = db;
        _hasher = hasher;
        _automacoes = automacoes;
        _log = log;
    }

    public async Task ExecutarAsync()
    {
        await CatalogoAsync();
        await CuponsAsync();
        await UsuariosAsync();
        await PedidosDeExemploAsync();
        await AssinaturasEDeliveryAsync();
        await OuvidoriaAsync();
    }

    private async Task CatalogoAsync()
    {
        if (!await _db.Categorias.AnyAsync())
        {
            _db.Categorias.AddRange(CatalogoSeed.Categorias.Select(c => new Categoria
            {
                Slug = c.Slug, Nome = c.Nome, Descricao = c.Descricao, Icone = c.Icone, Ordem = c.Ordem
            }));
            await _db.SaveChangesAsync();
        }

        if (await _db.Medicamentos.AnyAsync()) return;

        var categorias = await _db.Categorias.ToDictionaryAsync(c => c.Slug, c => c.IdCategoria);
        foreach (var p in CatalogoSeed.Produtos)
        {
            _db.Medicamentos.Add(new Medicamento
            {
                IdCategoria = categorias[p.Categoria], Nome = p.Nome, Slug = p.Slug, Tipo = p.Tipo,
                PrincipioAtivo = p.PrincipioAtivo, Descricao = p.Descricao, Dosagem = p.Dosagem,
                Apresentacao = p.Apresentacao, Fabricante = p.Fabricante, ViaAdministracao = p.Via,
                RegistroAnvisa = p.Anvisa, ValidadeLote = p.Validade, Preco = p.Preco, ExigeReceita = p.ExigeReceita,
                UsoContinuo = p.UsoContinuo, FarmaciaPopular = p.FarmaciaPopular,
                Estoque = new Estoque { Quantidade = p.Estoque, QuantidadeMinima = p.EstoqueMinimo, Localizacao = "Depósito" }
            });
        }
        await _db.SaveChangesAsync();
        _log.LogInformation("Catálogo inicial criado com {Total} produtos.", CatalogoSeed.Produtos.Length);
    }

    private async Task CuponsAsync()
    {
        if (await _db.Cupons.AnyAsync()) return;
        _db.Cupons.AddRange(CatalogoSeed.Cupons.Select(c => new Cupom
        {
            Codigo = c.Codigo, Descricao = c.Descricao, Tipo = c.Tipo, Valor = c.Valor, ValorMinimoPedido = c.ValorMinimo,
            DataInicio = new DateTime(2026, 1, 1), DataFim = new DateTime(2027, 12, 31, 23, 59, 59)
        }));
        await _db.SaveChangesAsync();
    }

    private async Task UsuariosAsync()
    {
        if (await _db.Usuarios.AnyAsync()) return;

        Usuario Novo(string nome, string? social, string email, string cpf, string tel, string senha, string perfil, DateTime nasc) => new()
        {
            Nome = nome, NomeSocial = social, Email = email, Cpf = cpf, Telefone = tel, SenhaHash = _hasher.Gerar(senha),
            Perfil = perfil, DataNascimento = nasc, CriadoEm = DateTime.Now.AddDays(-60)
        };

        _db.Usuarios.AddRange(
            Novo("Administrador Bem Estar", null, "admin@bemestaronline.com", "526.018.159-06", "(11) 90000-0000", "Admin@123", Perfis.Admin, new DateTime(1990, 1, 1)),
            Novo("Paula Atendente", null, "atendente@bemestaronline.com", "083.016.613-05", "(11) 90000-0001", "Atendente@123", Perfis.Atendente, new DateTime(1995, 3, 12)),
            Novo("Marcos Entregador", null, "entregador@bemestaronline.com", "543.231.948-97", "(11) 90000-0002", "Entregador@123", Perfis.Entregador, new DateTime(1993, 9, 2)),
            Novo("Cliente Demonstração", null, "cliente@bemestaronline.com", "186.091.390-34", "(11) 98888-7777", "Cliente@123", Perfis.Cliente, new DateTime(1998, 5, 20)),
            Novo("Maria das Graças Souza", null, "maria.souza@exemplo.com", "996.030.824-30", "(11) 97777-1111", "Cliente@123", Perfis.Cliente, new DateTime(1952, 8, 3)),
            Novo("José Carlos Pereira", null, "jose.pereira@exemplo.com", "628.194.821-12", "(11) 97777-2222", "Cliente@123", Perfis.Cliente, new DateTime(1961, 11, 21)),
            Novo("Rafael Lima", "Rafaela Lima", "rafaela.lima@exemplo.com", "993.518.190-19", "(11) 97777-3333", "Cliente@123", Perfis.Cliente, new DateTime(2000, 2, 14)),
            Novo("Ana Beatriz Nascimento", null, "ana.nascimento@exemplo.com", "937.865.797-41", "(11) 97777-4444", "Cliente@123", Perfis.Cliente, new DateTime(1988, 6, 30))
        );
        await _db.SaveChangesAsync();

        // Coordenadas aproximadas de bairros de Cajamar (para o mapa da entrega).
        var bairros = new[]
        {
            ("Centro", "07776-000", -23.35190, -46.87330),
            ("Polvilho", "07791-000", -23.40330, -46.84620),
            ("Jordanésia", "07776-200", -23.33250, -46.83680),
            ("Lago Azul", "07790-000", -23.37740, -46.86020),
            ("Ponunduva", "07780-000", -23.32210, -46.90390)
        };
        var clientes = await _db.Usuarios.Where(u => u.Perfil == Perfis.Cliente).OrderBy(u => u.IdUsuario).ToListAsync();
        for (var i = 0; i < clientes.Count; i++)
        {
            var (bairro, cep, lat, lng) = bairros[i % bairros.Length];
            _db.Enderecos.Add(new Endereco
            {
                IdUsuario = clientes[i].IdUsuario, Apelido = "Casa", Cep = cep, Logradouro = "Rua das Flores",
                Numero = (100 + i * 37).ToString(), Bairro = bairro, Cidade = "Cajamar", Estado = "SP", Principal = true,
                Latitude = lat, Longitude = lng
            });
        }
        await _db.SaveChangesAsync();
        _log.LogInformation("Usuários de teste criados (admin, atendente e clientes).");
    }

    /// <summary>Cria pedidos dos últimos 14 dias para o painel administrativo já abrir com dados.</summary>
    private async Task PedidosDeExemploAsync()
    {
        if (await _db.Pedidos.AnyAsync()) return;

        var clientes = await _db.Usuarios.Include(u => u.Enderecos).Where(u => u.Perfil == Perfis.Cliente).ToListAsync();
        var produtos = await _db.Medicamentos.Include(m => m.Estoque)
            .Where(m => m.Estoque != null && m.Estoque.Quantidade > 40).OrderBy(m => m.IdMedicamento).ToListAsync();
        if (clientes.Count == 0 || produtos.Count == 0) return;

        var entregador = await _db.Usuarios.FirstOrDefaultAsync(u => u.Perfil == Perfis.Entregador);
        var sorteio = new Random(2026);
        var metodos = new[] { MetodoPagamento.Pix, MetodoPagamento.CartaoCredito, MetodoPagamento.Pix, MetodoPagamento.CartaoDebito, MetodoPagamento.Dinheiro };
        var criados = new List<Pedido>();

        for (var diasAtras = 13; diasAtras >= 0; diasAtras--)
        {
            var quantidadeNoDia = sorteio.Next(1, 4);
            for (var n = 0; n < quantidadeNoDia; n++)
            {
                var cliente = clientes[sorteio.Next(clientes.Count)];
                var endereco = cliente.Enderecos.First();
                var data = DateTime.Today.AddDays(-diasAtras).AddHours(sorteio.Next(8, 20)).AddMinutes(sorteio.Next(0, 60));
                if (data > DateTime.Now) data = DateTime.Now.AddMinutes(-sorteio.Next(5, 90));

                var metodo = metodos[sorteio.Next(metodos.Length)];
                var pedido = new Pedido
                {
                    IdUsuario = cliente.IdUsuario, IdEndereco = endereco.IdEndereco, CriadoEm = data,
                    Canal = sorteio.Next(3) == 0 ? CanalVenda.App : CanalVenda.Site,
                    Status = metodo == MetodoPagamento.Dinheiro ? StatusPedido.AguardandoPagamento : StatusPedido.Pago
                };

                foreach (var produto in produtos.OrderBy(_ => sorteio.Next()).Take(sorteio.Next(1, 4)))
                {
                    var qtd = sorteio.Next(1, 3);
                    pedido.Itens.Add(new PedidoItem
                    {
                        IdMedicamento = produto.IdMedicamento, NomeProduto = produto.Nome, Quantidade = qtd,
                        PrecoUnitario = produto.Preco, Subtotal = produto.Preco * qtd
                    });
                    pedido.RequerReceita |= produto.ExigeReceita;
                }
                pedido.Subtotal = pedido.Itens.Sum(i => i.Subtotal);
                pedido.Frete = RegrasComerciais.CalcularFrete(pedido.Subtotal);
                pedido.Total = pedido.Subtotal + pedido.Frete;
                pedido.Pagamentos.Add(new Pagamento
                {
                    Metodo = metodo, Valor = pedido.Total, CriadoEm = data,
                    Status = metodo == MetodoPagamento.Dinheiro ? StatusPagamento.Pendente : StatusPagamento.Aprovado,
                    PagoEm = metodo == MetodoPagamento.Dinheiro ? null : data,
                    CodigoTransacao = $"BEO-DEMO-{data:yyyyMMddHHmm}-{n}"
                });

                _db.Pedidos.Add(pedido);
                await _db.SaveChangesAsync();               // no MySQL, aqui os triggers baixam o estoque
                await _automacoes.AposCriarPedidoAsync(pedido); // no modo memória, a mesma regra em C#

                // Avança o pedido conforme a "idade" dele, passo a passo (cada passo dispara o trigger de histórico).
                var caminho = diasAtras switch
                {
                    >= 3 when sorteio.Next(10) == 0 => new[] { StatusPedido.Cancelado },
                    >= 3 => new[] { StatusPedido.EmSeparacao, StatusPedido.SaiuParaEntrega, StatusPedido.Entregue },
                    2 or 1 => new[] { StatusPedido.EmSeparacao, StatusPedido.SaiuParaEntrega },
                    0 when n == 0 => new[] { StatusPedido.EmSeparacao }, // já disponível para o entregador de teste
                    _ => Array.Empty<string>()
                };
                foreach (var proximo in caminho)
                {
                    if (proximo == StatusPedido.SaiuParaEntrega && entregador is not null)
                    {
                        // Pedido pego pelo entregador de teste; posição a meio caminho entre a loja e o cliente.
                        pedido.IdEntregador = entregador.IdUsuario;
                        pedido.AtualizarLocalizacao(
                            (RegrasComerciais.LojaLatitude + (endereco.Latitude ?? RegrasComerciais.LojaLatitude)) / 2,
                            (RegrasComerciais.LojaLongitude + (endereco.Longitude ?? RegrasComerciais.LojaLongitude)) / 2);
                    }
                    if (proximo == StatusPedido.Entregue) pedido.ReceitaConferida = pedido.RequerReceita;
                    var anterior = pedido.AlterarStatus(proximo);
                    await _db.SaveChangesAsync();
                    await _automacoes.AposAlterarStatusAsync(pedido, anterior);
                }
                criados.Add(pedido);
            }
        }

        // Ajusta os horários do histórico para ficarem coerentes com a data do pedido
        // (no MySQL o trigger de UPDATE grava a hora atual, o que é certo no uso real, mas não para dados de exemplo).
        var ids = criados.Select(p => p.IdPedido).ToHashSet();
        var historicos = (await _db.PedidoHistorico.ToListAsync()).Where(h => ids.Contains(h.IdPedido)).GroupBy(h => h.IdPedido);
        foreach (var grupo in historicos)
        {
            var pedido = criados.First(p => p.IdPedido == grupo.Key);
            var passo = 0;
            foreach (var h in grupo.OrderBy(h => h.IdHistorico))
            {
                var momento = pedido.CriadoEm.AddHours(passo * 5);
                h.AlteradoEm = momento > DateTime.Now ? DateTime.Now : momento;
                passo++;
            }
        }
        await _db.SaveChangesAsync();
        _log.LogInformation("{Total} pedidos de exemplo criados.", criados.Count);
    }

    private async Task AssinaturasEDeliveryAsync()
    {
        if (!await _db.Assinaturas.AnyAsync())
        {
            var clientes = await _db.Usuarios.Where(u => u.Perfil == Perfis.Cliente).OrderBy(u => u.IdUsuario).ToListAsync();
            var continuos = await _db.Medicamentos.Where(m => m.UsoContinuo && m.ExigeReceita).OrderBy(m => m.IdMedicamento).Take(4).ToListAsync();
            if (clientes.Count >= 2 && continuos.Count >= 3)
            {
                _db.Assinaturas.AddRange(
                    new Assinatura { IdUsuario = clientes[1].IdUsuario, IdMedicamento = continuos[0].IdMedicamento, NomePaciente = clientes[1].Nome,
                                     Quantidade = 1, PeriodicidadeDias = 30, ValorUnitario = continuos[0].Preco, ProximaEntrega = DateTime.Today.AddDays(6) },
                    new Assinatura { IdUsuario = clientes[1].IdUsuario, IdMedicamento = continuos[2].IdMedicamento, NomePaciente = clientes[1].Nome,
                                     Quantidade = 2, PeriodicidadeDias = 30, ValorUnitario = continuos[2].Preco, ProximaEntrega = DateTime.Today.AddDays(6) },
                    new Assinatura { IdUsuario = clientes[2].IdUsuario, IdMedicamento = continuos[1].IdMedicamento, NomePaciente = "Helena Pereira (mãe)",
                                     Quantidade = 1, PeriodicidadeDias = 60, ValorUnitario = continuos[1].Preco, ProximaEntrega = DateTime.Today.AddDays(20) });
            }
        }

        if (!await _db.DeliverySolicitacoes.AnyAsync())
        {
            _db.DeliverySolicitacoes.AddRange(
                new DeliverySolicitacao { NomeCliente = "Joana Ribeiro", Telefone = "(11) 96666-1212", EnderecoTexto = "Av. Tenente Marques, 900 - Polvilho, Cajamar",
                                          Observacao = "Preciso do antibiótico da receita em anexo e de um termômetro.", ReceitaArquivo = "receita-joana.jpg",
                                          CriadoEm = DateTime.Now.AddHours(-3) },
                new DeliverySolicitacao { NomeCliente = "Seu Antônio", Telefone = "(11) 96666-3434", EnderecoTexto = "Rua Sete de Setembro, 45 - Centro, Cajamar",
                                          Observacao = "Fralda geriátrica tamanho G e pomada para assadura.", Status = StatusDelivery.EmAnalise,
                                          CriadoEm = DateTime.Now.AddDays(-1) });
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>Manifestações de exemplo para o painel da ouvidoria (RF30).</summary>
    private async Task OuvidoriaAsync()
    {
        if (await _db.Ouvidorias.AnyAsync()) return;
        var cliente = await _db.Usuarios.Where(u => u.Perfil == Perfis.Cliente).OrderBy(u => u.IdUsuario).Skip(1).FirstOrDefaultAsync();

        var denuncia = Ouvidoria.Registrar(TipoOuvidoria.Discriminacao, "RACIAL",
            "Fui seguida pelo segurança dentro da loja enquanto escolhia um protetor solar, sem motivo nenhum. Ninguém mais foi seguido.",
            anonimo: true, idUsuario: null, contato: null);
        denuncia.CriadoEm = DateTime.Now.AddHours(-5);

        var atendimento = Ouvidoria.Registrar(TipoOuvidoria.Atendimento, null,
            "Liguei duas vezes para saber da minha receita e ninguém atendeu no horário de almoço.",
            anonimo: false, idUsuario: cliente?.IdUsuario, contato: cliente?.Telefone);
        atendimento.CriadoEm = DateTime.Now.AddDays(-3);
        atendimento.Atualizar(StatusOuvidoria.Respondida,
            "Pedimos desculpas. Agora temos uma pessoa só para o telefone das 12h às 14h. Obrigado por avisar!");

        var elogio = Ouvidoria.Registrar(TipoOuvidoria.Elogio, null,
            "O entregador foi muito educado e me explicou direitinho como tomar o antibiótico.",
            anonimo: false, idUsuario: null, contato: null);
        elogio.CriadoEm = DateTime.Now.AddDays(-6);

        _db.Ouvidorias.AddRange(denuncia, atendimento, elogio);
        await _db.SaveChangesAsync();
        _log.LogInformation("Manifestações de exemplo da ouvidoria criadas.");
    }
}
