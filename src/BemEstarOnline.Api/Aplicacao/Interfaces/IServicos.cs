using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Interfaces;

/*
 * Contratos dos casos de uso. Os Controllers (camada de apresentação) só conversam
 * com estas interfaces, nunca com o banco diretamente.
 */

public interface ISenhaHasher
{
    string Gerar(string senha);
    bool Verificar(string senha, string hashSalvo);
}

public interface ITokenServico
{
    (string token, DateTime expiraEm) Gerar(Usuario usuario);
}

public interface IAuthServico
{
    Task<LoginRespostaDto> LoginAsync(LoginDto dto);
    Task<LoginRespostaDto> RegistrarAsync(RegistroDto dto);
    Task<UsuarioDto> ObterAsync(int idUsuario);
    Task ExcluirContaAsync(int idUsuario, string senha);
    Task<UsuarioDto> AlterarSenhaAsync(int idUsuario, AlterarSenhaDto dto);
}

/// <summary>E-mail a enviar. Link = o endereço principal da mensagem (o modo demonstração mostra no log).</summary>
public record MensagemEmail(string Para, string Assunto, string Html, string? Link = null);

/// <summary>
/// Envio de e-mail (RF33). Duas implementações: EmailSmtp (servidor de e-mail de verdade, se a seção "Smtp"
/// do appsettings estiver preenchida) e EmailDemonstracao (padrão: salva o e-mail como arquivo .html).
/// </summary>
public interface IEmailServico
{
    Task EnviarAsync(MensagemEmail mensagem);
}

/// <summary>"Esqueci minha senha" (RF33): link por e-mail, de uso único, válido por 30 minutos.</summary>
public interface IRecuperacaoSenhaServico
{
    Task SolicitarAsync(string email);
    Task RedefinirAsync(string token, string novaSenha);
}

/// <summary>Gestão da equipe pelo administrador (RF35).</summary>
public interface IEquipeServico
{
    Task<List<EquipeDto>> ListarAsync();
    Task<EquipeDto> CriarAsync(EquipeCriarDto dto);
    Task<EquipeDto> AtualizarAsync(int idAdmin, int idUsuario, EquipeAtualizarDto dto);
    Task<EquipeDto> RedefinirSenhaAsync(int idAdmin, int idUsuario, string novaSenha);
}

public interface ICatalogoServico
{
    Task<List<CategoriaDto>> ListarCategoriasAsync();
    Task<List<ProdutoDto>> ListarProdutosAsync(FiltroProdutos filtro);
    Task<ProdutoDto> ObterProdutoAsync(string idOuSlug);
    Task<ProdutoDto> CriarProdutoAsync(ProdutoSalvarDto dto);
    Task<ProdutoDto> AtualizarProdutoAsync(int id, ProdutoSalvarDto dto);
    Task<ProdutoDto> AjustarEstoqueAsync(int id, EstoqueAjusteDto dto);
    Task<ProdutoDto> DefinirImagemAsync(int id, string? imagemUrl);
}

public interface ICarrinhoServico
{
    Task<CarrinhoDto> ObterAsync(int idUsuario);
    Task<CarrinhoDto> SubstituirAsync(int idUsuario, List<ItemQuantidadeDto> itens);
    Task LimparAsync(int idUsuario);
}

public interface IEnderecoServico
{
    Task<List<EnderecoDto>> ListarAsync(int idUsuario);
    Task<EnderecoDto> CriarAsync(int idUsuario, EnderecoSalvarDto dto);
    Task RemoverAsync(int idUsuario, int idEndereco);
}

public interface IPedidoServico
{
    Task<ResumoValoresDto> SimularAsync(SimulacaoPedidoDto dto);
    Task<CupomResultadoDto> ValidarCupomAsync(CupomValidarDto dto);
    Task<PedidoDetalheDto> CriarAsync(int idUsuario, PedidoCriarDto dto);
    Task<List<PedidoResumoDto>> ListarDoUsuarioAsync(int idUsuario);
    Task<PedidoDetalheDto> ObterAsync(int idPedido, int idUsuarioSolicitante, bool solicitanteEhEquipe);
    Task<PedidoDetalheDto> CancelarAsync(int idPedido, int idUsuario);
    Task<PedidoDetalheDto> AlterarStatusAsync(int idPedido, string novoStatus);
    Task<List<PedidoResumoDto>> ListarAsync(string? status);
    Task<RastreioDto> RastreioAsync(int idPedido, int idUsuarioSolicitante, bool solicitanteEhEquipe);
}

public interface IEntregaServico
{
    Task<List<EntregaDto>> ListarAsync(int idEntregador);
    Task<EntregaDto> AceitarAsync(int idPedido, int idEntregador);
    Task<EntregaDto> SairParaEntregaAsync(int idPedido, int idEntregador);
    Task AtualizarLocalizacaoAsync(int idPedido, int idEntregador, LocalizacaoDto dto);
    Task<EntregaDto> ConfirmarEntregaAsync(int idPedido, int idEntregador, EntregarDto dto);
}

public interface IAssinaturaServico
{
    Task<AssinaturaDto> CriarAsync(int idUsuario, AssinaturaCriarDto dto);
    Task<List<AssinaturaDto>> ListarDoUsuarioAsync(int idUsuario);
    Task<List<AssinaturaDto>> ListarTodasAsync();
    Task<AssinaturaDto> AlterarStatusAsync(int idAssinatura, string status, int idUsuarioSolicitante, bool solicitanteEhEquipe);
}

/// <summary>Arquivo que chegou na requisição (a camada de aplicação não depende do IFormFile do ASP.NET).</summary>
public record ArquivoRecebido(string Nome, long Tamanho, Func<Stream> Abrir);

/// <summary>Arquivo lido da pasta privada, pronto para devolver à equipe.</summary>
public record ArquivoGuardado(Stream Conteudo, string TipoConteudo);

/// <summary>
/// Pasta privada da API (RF39): fica fora do wwwroot e do site, então nada aqui tem endereço público.
/// Só a API lê, e só para a equipe logada. Receita é dado de saúde (dado sensível na LGPD).
/// </summary>
public interface IArquivosPrivados
{
    /// <summary>Guarda com um nome aleatório (GUID) e devolve esse nome.</summary>
    Task<string> GuardarReceitaAsync(Stream conteudo, string extensao);
    ArquivoGuardado? AbrirReceita(string nome);
    void ApagarReceita(string nome);
}

public interface IDeliveryServico
{
    Task<DeliveryDto> CriarAsync(int? idUsuario, DeliveryCriarDto dto, ArquivoRecebido? receita = null);
    /// <summary>Só a equipe: abre a foto/PDF da receita da solicitação.</summary>
    Task<ArquivoGuardado> AbrirReceitaAsync(int idDelivery);
    Task<List<DeliveryDto>> ListarDoUsuarioAsync(int idUsuario);
    Task<List<DeliveryDto>> ListarTodasAsync();
    Task<DeliveryDto> AlterarStatusAsync(int idDelivery, string status);
}

public interface IOuvidoriaServico
{
    Task<OuvidoriaProtocoloDto> RegistrarAsync(int? idUsuario, OuvidoriaCriarDto dto);
    Task<OuvidoriaProtocoloDto> ConsultarAsync(string protocolo);
    Task<List<OuvidoriaDto>> ListarAsync();
    Task<OuvidoriaDto> AtualizarAsync(int idOuvidoria, OuvidoriaAtualizarDto dto);
}

public interface IAdminServico
{
    Task<PainelDto> PainelAsync();
    Task<List<ClienteDto>> ClientesAsync();
    Task<List<AlertaDto>> AlertasAsync();
    Task ResolverAlertaAsync(int idAlerta);
}
