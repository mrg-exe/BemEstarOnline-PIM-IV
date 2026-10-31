using System.ComponentModel.DataAnnotations;

namespace BemEstarOnline.Api.Aplicacao.Dtos;

/*
 * DTOs (Data Transfer Objects): o "formato" dos dados que entram e saem da API.
 * Assim as entidades do banco nunca são expostas diretamente (ex.: o hash da senha).
 * O site e o app mobile usam exatamente estes formatos, em JSON.
 */

// ---------------- Autenticação ----------------
public record LoginDto(
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")] string Email,
    [Required(ErrorMessage = "Informe a senha.")] string Senha);

public record RegistroDto(
    [Required(ErrorMessage = "Informe o nome."), MaxLength(120)] string Nome,
    [MaxLength(120)] string? NomeSocial,
    DateTime? DataNascimento,
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")] string Email,
    [Required(ErrorMessage = "Informe o CPF.")] string Cpf,
    string? Telefone,
    [Required, MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")] string Senha);

/// <summary>TrocarSenha = true: senha provisória dada pelo administrador; site e app abrem a tela de nova senha.</summary>
public record UsuarioDto(int IdUsuario, string Nome, string? NomeSocial, string NomeExibicao, string Email,
                         string Cpf, string? Telefone, string Perfil, bool TrocarSenha);

public record AlterarSenhaDto(
    [Required(ErrorMessage = "Digite a senha atual.")] string SenhaAtual,
    [Required, MinLength(6, ErrorMessage = "A nova senha precisa ter pelo menos 6 caracteres.")] string NovaSenha);

// ---------------- Esqueci minha senha (RF33) ----------------
public record EsqueciSenhaDto(
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")] string Email);

public record RecuperarSenhaDto(
    [Required(ErrorMessage = "Link inválido. Peça um novo.")] string Token,
    [Required, MinLength(6, ErrorMessage = "A nova senha precisa ter pelo menos 6 caracteres.")] string NovaSenha);

// ---------------- Gestão da equipe (RF35, só ADMIN) ----------------
public record EquipeDto(int IdUsuario, string Nome, string NomeExibicao, string Email, string Cpf, string? Telefone,
                        string Perfil, bool Ativo, bool TrocarSenha, DateTime CriadoEm);

public record EquipeCriarDto(
    [Required(ErrorMessage = "Informe o nome."), MaxLength(120)] string Nome,
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")] string Email,
    [Required(ErrorMessage = "Informe o CPF.")] string Cpf,
    string? Celular,
    [Required(ErrorMessage = "Escolha o perfil.")] string Perfil,
    [Required, MinLength(6, ErrorMessage = "A senha inicial precisa ter pelo menos 6 caracteres.")] string SenhaInicial);

/// <summary>Muda o perfil e/ou ativa/desativa a conta (campos nulos ficam como estão).</summary>
public record EquipeAtualizarDto(string? Perfil, bool? Ativo);

public record RedefinirSenhaDto(
    [Required, MinLength(6, ErrorMessage = "A senha provisória precisa ter pelo menos 6 caracteres.")] string NovaSenha);

public record LoginRespostaDto(string Token, DateTime ExpiraEm, UsuarioDto Usuario);

// ---------------- Catálogo ----------------
public record CategoriaDto(int IdCategoria, string Nome, string Slug, string? Descricao, string? Icone, int TotalProdutos);

public record ProdutoDto(int IdMedicamento, string Nome, string Slug, int IdCategoria, string Categoria, string CategoriaSlug,
    string Tipo, string? PrincipioAtivo, string Descricao, string? Dosagem, string? Apresentacao, string Fabricante,
    string? ViaAdministracao, string? RegistroAnvisa, DateTime? ValidadeLote, decimal Preco, bool ExigeReceita,
    bool UsoContinuo, bool FarmaciaPopular, int Estoque, int EstoqueMinimo, bool Disponivel, bool Ativo, string? ImagemUrl);

public record ProdutoSalvarDto(
    [Required(ErrorMessage = "Informe o nome."), MaxLength(180)] string Nome,
    int IdCategoria,
    [Required(ErrorMessage = "Informe o tipo.")] string Tipo,
    string? PrincipioAtivo,
    [Required(ErrorMessage = "Informe a descrição.")] string Descricao,
    string? Dosagem, string? Apresentacao,
    [Required(ErrorMessage = "Informe o fabricante.")] string Fabricante,
    string? ViaAdministracao, string? RegistroAnvisa, DateTime? ValidadeLote,
    [Range(0.01, 99999, ErrorMessage = "O preço precisa ser maior que zero.")] decimal Preco,
    bool ExigeReceita, bool UsoContinuo, bool FarmaciaPopular, bool Ativo,
    int? EstoqueInicial, int? EstoqueMinimo, [MaxLength(255)] string? ImagemUrl);

public record EstoqueAjusteDto([Range(0, 100000)] int Quantidade, [Range(0, 100000)] int? QuantidadeMinima);

// ---------------- Carrinho e pedidos ----------------
public record ItemQuantidadeDto(int IdMedicamento, [Range(1, 99, ErrorMessage = "Quantidade entre 1 e 99.")] int Quantidade);

public record CarrinhoItemDto(int IdMedicamento, string Nome, string Slug, decimal PrecoUnitario, int Quantidade,
                              decimal Subtotal, bool ExigeReceita, int EstoqueDisponivel);

public record CarrinhoDto(List<CarrinhoItemDto> Itens, decimal Subtotal, int QuantidadeTotal);

public record EnderecoDto(int IdEndereco, string? Apelido, string Cep, string Logradouro, string Numero, string? Complemento,
                          string Bairro, string Cidade, string Estado, bool Principal, string TextoCompleto,
                          double? Latitude, double? Longitude);

public record EnderecoSalvarDto(
    string? Apelido,
    [Required(ErrorMessage = "Informe o CEP.")] string Cep,
    [Required(ErrorMessage = "Informe a rua.")] string Logradouro,
    [Required(ErrorMessage = "Informe o número.")] string Numero,
    string? Complemento,
    [Required(ErrorMessage = "Informe o bairro.")] string Bairro,
    [Required(ErrorMessage = "Informe a cidade.")] string Cidade,
    [Required(ErrorMessage = "Informe o estado."), StringLength(2, MinimumLength = 2, ErrorMessage = "Use a sigla do estado (ex.: SP).")] string Estado,
    bool Principal,
    double? Latitude = null,
    double? Longitude = null);

public record SimulacaoPedidoDto([Required] List<ItemQuantidadeDto> Itens, string? CodigoCupom);

public record ResumoValoresDto(List<CarrinhoItemDto> Itens, decimal Subtotal, decimal Desconto, decimal Frete, decimal Total,
                               string? CupomAplicado, string? MensagemCupom, bool RequerReceita, decimal FaltaParaFreteGratis);

public record PedidoCriarDto(
    [Required] List<ItemQuantidadeDto> Itens,
    int? IdEndereco,
    EnderecoSalvarDto? NovoEndereco,
    string? CodigoCupom,
    [Required(ErrorMessage = "Escolha a forma de pagamento.")] string MetodoPagamento,
    [MaxLength(255)] string? Observacao,
    string? Canal);

public record PedidoResumoDto(int IdPedido, string Status, string Canal, decimal Total, int QuantidadeItens, DateTime CriadoEm,
                              string? Cliente);

public record PedidoItemDto(int IdMedicamento, string NomeProduto, int Quantidade, decimal PrecoUnitario, decimal Subtotal);
public record PagamentoDto(string Metodo, string Status, decimal Valor, string? CodigoTransacao, DateTime? PagoEm);
public record HistoricoDto(string? StatusAnterior, string StatusNovo, DateTime AlteradoEm);

public record PedidoDetalheDto(int IdPedido, string Status, string Canal, decimal Subtotal, decimal Desconto, decimal Frete,
    decimal Total, bool RequerReceita, string? Observacao, DateTime CriadoEm, string Cliente, string? TelefoneCliente,
    EnderecoDto? Endereco, List<PedidoItemDto> Itens, PagamentoDto? Pagamento, List<HistoricoDto> Historico,
    bool PodeCancelar, IReadOnlyList<string> ProximosStatus, string? Entregador, bool ReceitaConferida);

public record AlterarStatusDto([Required] string Status);

public record CupomValidarDto([Required] string Codigo, decimal Subtotal);
public record CupomResultadoDto(bool Valido, string Mensagem, decimal Desconto, decimal Frete, decimal Total);

// ---------------- Assinaturas e delivery ----------------
public record AssinaturaCriarDto(
    int IdMedicamento,
    [Required(ErrorMessage = "Informe o nome do paciente.")] string NomePaciente,
    [Range(1, 10, ErrorMessage = "Quantidade entre 1 e 10 caixas.")] int Quantidade,
    int PeriodicidadeDias);

public record AssinaturaDto(int IdAssinatura, int IdMedicamento, string Medicamento, string NomePaciente, int Quantidade,
    int PeriodicidadeDias, decimal ValorUnitario, decimal DescontoPercentual, decimal ValorPorEntrega, string Status,
    DateTime? ProximaEntrega, DateTime CriadaEm, string? Cliente);

public record DeliveryCriarDto(
    [Required(ErrorMessage = "Informe seu nome.")] string NomeCliente,
    [Required(ErrorMessage = "Informe um telefone.")] string Telefone,
    [Required(ErrorMessage = "Informe o endereço.")] string EnderecoTexto,
    [Required(ErrorMessage = "Conte o que você precisa.")] string Observacao);

public record DeliveryDto(int IdDelivery, string NomeCliente, string Telefone, string EnderecoTexto, string? Observacao,
                          string? ReceitaArquivo, string Status, DateTime CriadoEm);

// ---------------- Ouvidoria e canal de denúncia (RF30) ----------------
public record OuvidoriaCriarDto(
    [Required(ErrorMessage = "Escolha o tipo de manifestação.")] string Tipo,
    string? SubtipoDiscriminacao,
    [Required(ErrorMessage = "Conte o que aconteceu."), MaxLength(4000, ErrorMessage = "Use no máximo 4000 letras.")] string Descricao,
    bool Anonimo,
    [MaxLength(150, ErrorMessage = "Contato muito longo.")] string? Contato);

/// <summary>O que qualquer pessoa vê com o protocolo: só a situação e a resposta (nunca o relato nem o contato).</summary>
public record OuvidoriaProtocoloDto(string Protocolo, string Tipo, string Status, DateTime CriadoEm, string? Resposta,
                                    DateTime? RespondidoEm);

/// <summary>Visão completa, só para a equipe.</summary>
public record OuvidoriaDto(int IdOuvidoria, string Protocolo, string Tipo, string? SubtipoDiscriminacao, string Descricao,
    bool Anonimo, string? Cliente, string? Contato, string Status, string? Resposta, DateTime CriadoEm, DateTime? RespondidoEm);

public record OuvidoriaAtualizarDto(string? Status, [MaxLength(4000, ErrorMessage = "Use no máximo 4000 letras.")] string? Resposta);

// ---------------- Entrega e rastreamento ----------------
/// <summary>Posição para o mapa do cliente (atualizada pelo app do entregador).</summary>
public record RastreioDto(int IdPedido, string Status, string? Entregador, double? EntregadorLatitude, double? EntregadorLongitude,
    DateTime? LocalizacaoEm, double LojaLatitude, double LojaLongitude, double? DestinoLatitude, double? DestinoLongitude,
    string? EnderecoTexto);

/// <summary>Pedido visto pelo entregador.</summary>
public record EntregaDto(int IdPedido, string Status, string Cliente, string? Telefone, string EnderecoTexto,
    double? DestinoLatitude, double? DestinoLongitude, bool RequerReceita, bool ReceitaConferida, decimal Total,
    string FormaPagamento, bool CobrarNaEntrega, string? Observacao, List<PedidoItemDto> Itens, DateTime CriadoEm,
    bool MinhaEntrega);

public record LocalizacaoDto([Range(-90, 90)] double Latitude, [Range(-180, 180)] double Longitude);
public record EntregarDto(bool ReceitaConferida);
public record ExcluirContaDto([Required(ErrorMessage = "Digite sua senha para confirmar.")] string Senha);

// ---------------- Área administrativa ----------------
public record VendaDiaDto(DateTime Dia, int QuantidadePedidos, decimal Faturamento);
public record EstoqueBaixoDto(int IdMedicamento, string Nome, int Quantidade, int QuantidadeMinima);
public record VencendoDto(int IdMedicamento, string Nome, DateTime ValidadeLote, int DiasRestantes);

public record PainelDto(decimal VendasHoje, decimal VendasMes, int PedidosHoje, int PedidosEmAberto, int TotalClientes,
    int AssinaturasAtivas, int SolicitacoesAbertas, List<VendaDiaDto> VendasPorDia, List<EstoqueBaixoDto> EstoqueBaixo,
    List<VencendoDto> ProdutosVencendo, string ModoBanco);

public record ClienteDto(int IdUsuario, string NomeExibicao, string Email, string? Telefone, string Perfil, bool Ativo,
                         int TotalPedidos, decimal TotalGasto, DateTime CriadoEm);

public record AlertaDto(int IdAlerta, int IdMedicamento, string Medicamento, int QuantidadeAtual, int QuantidadeMinima,
                        int EstoqueAgora, DateTime CriadoEm);
