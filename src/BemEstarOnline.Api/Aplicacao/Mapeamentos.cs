using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;

namespace BemEstarOnline.Api.Aplicacao;

/// <summary>Converte entidades do domínio em DTOs de resposta.</summary>
public static class Mapeamentos
{
    public static UsuarioDto ParaDto(this Usuario u) =>
        new(u.IdUsuario, u.Nome, u.NomeSocial, u.NomeExibicao, u.Email, u.Cpf, u.Telefone, u.Perfil, u.TrocarSenha);

    public static EquipeDto ParaEquipeDto(this Usuario u) =>
        new(u.IdUsuario, u.Nome, u.NomeExibicao, u.Email, u.Cpf, u.Telefone, u.Perfil, u.Ativo, u.TrocarSenha, u.CriadoEm);

    public static ProdutoDto ParaDto(this Medicamento m) =>
        new(m.IdMedicamento, m.Nome, m.Slug, m.IdCategoria, m.Categoria?.Nome ?? "", m.Categoria?.Slug ?? "",
            m.Tipo, m.PrincipioAtivo, m.Descricao, m.Dosagem, m.Apresentacao, m.Fabricante, m.ViaAdministracao,
            m.RegistroAnvisa, m.ValidadeLote, m.Preco, m.ExigeReceita, m.UsoContinuo, m.FarmaciaPopular,
            m.QuantidadeEmEstoque, m.Estoque?.QuantidadeMinima ?? 0, m.Disponivel, m.Ativo, m.ImagemUrl);

    public static EnderecoDto ParaDto(this Endereco e) =>
        new(e.IdEndereco, e.Apelido, e.Cep, e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Estado,
            e.Principal, e.TextoCompleto, e.Latitude, e.Longitude);

    public static PedidoResumoDto ParaResumo(this Pedido p) =>
        new(p.IdPedido, p.Status, p.Canal, p.Total, p.Itens.Sum(i => i.Quantidade), p.CriadoEm, p.Usuario?.NomeExibicao);

    public static PedidoDetalheDto ParaDetalhe(this Pedido p)
    {
        var pagamento = p.Pagamentos.OrderByDescending(x => x.IdPagamento).FirstOrDefault();
        return new PedidoDetalheDto(
            p.IdPedido, p.Status, p.Canal, p.Subtotal, p.Desconto, p.Frete, p.Total, p.RequerReceita, p.Observacao,
            p.CriadoEm, p.Usuario?.NomeExibicao ?? "", p.Usuario?.Telefone, p.Endereco?.ParaDto(),
            p.Itens.Select(i => new PedidoItemDto(i.IdMedicamento, i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList(),
            pagamento is null ? null : new PagamentoDto(pagamento.Metodo, pagamento.Status, pagamento.Valor, pagamento.CodigoTransacao, pagamento.PagoEm),
            p.Historico.OrderBy(h => h.AlteradoEm).ThenBy(h => h.IdHistorico)
                       .Select(h => new HistoricoDto(h.StatusAnterior, h.StatusNovo, h.AlteradoEm)).ToList(),
            p.ClientePodeCancelar,
            StatusPedido.ProximosPossiveis(p.Status),
            p.Entregador?.NomeExibicao,
            p.ReceitaConferida);
    }

    public static RastreioDto ParaRastreio(this Pedido p) =>
        new(p.IdPedido, p.Status, p.Entregador?.NomeExibicao, p.EntregadorLatitude, p.EntregadorLongitude, p.LocalizacaoEm,
            RegrasComerciais.LojaLatitude, RegrasComerciais.LojaLongitude, p.Endereco?.Latitude, p.Endereco?.Longitude,
            p.Endereco?.TextoCompleto);

    public static EntregaDto ParaEntrega(this Pedido p, int idEntregador)
    {
        var pagamento = p.Pagamentos.OrderByDescending(x => x.IdPagamento).FirstOrDefault();
        return new EntregaDto(p.IdPedido, p.Status, p.Usuario?.NomeExibicao ?? "", p.Usuario?.Telefone,
            p.Endereco?.TextoCompleto ?? "", p.Endereco?.Latitude, p.Endereco?.Longitude, p.RequerReceita, p.ReceitaConferida,
            p.Total, pagamento?.Metodo ?? "", pagamento?.Status == StatusPagamento.Pendente, p.Observacao,
            p.Itens.Select(i => new PedidoItemDto(i.IdMedicamento, i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList(),
            p.CriadoEm, p.IdEntregador == idEntregador);
    }

    public static AssinaturaDto ParaDto(this Assinatura a) =>
        new(a.IdAssinatura, a.IdMedicamento, a.Medicamento?.Nome ?? "", a.NomePaciente, a.Quantidade, a.PeriodicidadeDias,
            a.ValorUnitario, a.DescontoPercentual, a.ValorPorEntrega, a.Status, a.ProximaEntrega, a.CriadaEm,
            a.Usuario?.NomeExibicao);

    public static DeliveryDto ParaDto(this DeliverySolicitacao d) =>
        new(d.IdDelivery, d.NomeCliente, d.Telefone, d.EnderecoTexto, d.Observacao, d.ReceitaArquivo, d.Status, d.CriadoEm);

    public static OuvidoriaProtocoloDto ParaProtocoloDto(this Ouvidoria o) =>
        new(o.Protocolo, o.Tipo, o.Status, o.CriadoEm, o.Resposta, o.RespondidoEm);

    public static OuvidoriaDto ParaDto(this Ouvidoria o) =>
        new(o.IdOuvidoria, o.Protocolo, o.Tipo, o.SubtipoDiscriminacao, o.Descricao, o.Anonimo,
            o.Anonimo ? null : o.Usuario?.NomeExibicao, o.Contato, o.Status, o.Resposta, o.CriadoEm, o.RespondidoEm);
}
