using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>Login, cadastro e consulta do usuário logado.</summary>
public class AuthServico : IAuthServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly ISenhaHasher _hasher;
    private readonly ITokenServico _tokens;
    private readonly IUnidadeDeTrabalho _uow;
    private readonly IEnderecoRepositorio _enderecos;
    private readonly IAssinaturaRepositorio _assinaturas;
    private readonly ICarrinhoServico _carrinho;
    private readonly IDeliveryRepositorio _delivery;
    private readonly IArquivosPrivados _arquivos;

    public AuthServico(IUsuarioRepositorio usuarios, ISenhaHasher hasher, ITokenServico tokens, IUnidadeDeTrabalho uow,
                       IEnderecoRepositorio enderecos, IAssinaturaRepositorio assinaturas, ICarrinhoServico carrinho,
                       IDeliveryRepositorio delivery, IArquivosPrivados arquivos)
    {
        _delivery = delivery;
        _arquivos = arquivos;
        _usuarios = usuarios;
        _hasher = hasher;
        _tokens = tokens;
        _uow = uow;
        _enderecos = enderecos;
        _assinaturas = assinaturas;
        _carrinho = carrinho;
    }

    /// <summary>
    /// O próprio cliente exclui a conta (sem precisar falar com o suporte).
    /// Apaga carrinho e endereços, cancela assinaturas e anonimiza o cadastro.
    /// Os pedidos antigos ficam, sem dados pessoais, porque a farmácia precisa guardá-los (obrigação fiscal).
    /// </summary>
    public async Task ExcluirContaAsync(int idUsuario, string senha)
    {
        var usuario = await _usuarios.ObterPorIdAsync(idUsuario) ?? throw new NaoEncontradoException("Conta não encontrada.");
        if (!_hasher.Verificar(senha, usuario.SenhaHash)) throw new RegraDeNegocioException("Senha incorreta.");
        if (usuario.Perfil != Perfis.Cliente)
            throw new RegraDeNegocioException("Contas da equipe são desativadas pelo administrador da farmácia.");

        await _carrinho.LimparAsync(idUsuario);

        foreach (var assinatura in await _assinaturas.ListarParaAlterarAsync(idUsuario))
            if (assinatura.Status != StatusAssinatura.Cancelada) assinatura.Status = StatusAssinatura.Cancelada;

        foreach (var endereco in await _enderecos.ListarPorUsuarioAsync(idUsuario))
        {
            if (await _enderecos.EmUsoEmPedidoAsync(endereco.IdEndereco))
            {
                // Usado em pedido: fica só o bairro/cidade, sem rua e número.
                endereco.Apelido = null;
                endereco.Logradouro = "Endereço removido";
                endereco.Numero = "-";
                endereco.Complemento = null;
                endereco.Latitude = null;
                endereco.Longitude = null;
            }
            else
            {
                _enderecos.Remover(endereco);
            }
        }

        // Receitas enviadas (RF39) são dado de saúde: a solicitação fica sem dados pessoais e o arquivo é apagado.
        var receitas = new List<string>();
        foreach (var solicitacao in await _delivery.ListarParaAlterarAsync(idUsuario))
        {
            if (solicitacao.ReceitaArquivo is not null) receitas.Add(solicitacao.ReceitaArquivo);
            solicitacao.Anonimizar();
        }

        usuario.Anonimizar(_hasher.Gerar(Guid.NewGuid().ToString("N")));
        await _uow.SalvarAsync();
        foreach (var receita in receitas) _arquivos.ApagarReceita(receita);
    }

    public async Task<LoginRespostaDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _usuarios.ObterPorEmailAsync(dto.Email.Trim().ToLowerInvariant());

        // Mensagem genérica de propósito: não revelamos se o e-mail existe ou não.
        if (usuario is null || !usuario.Ativo || !_hasher.Verificar(dto.Senha, usuario.SenhaHash))
            throw new RegraDeNegocioException("E-mail ou senha incorretos.");

        return GerarResposta(usuario);
    }

    public async Task<LoginRespostaDto> RegistrarAsync(RegistroDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var cpf = ValidadorCpf.Formatar(dto.Cpf);

        if (!ValidadorCpf.EhValido(cpf)) throw new RegraDeNegocioException("CPF inválido. Confira os números.");
        if (await _usuarios.EmailExisteAsync(email)) throw new RegraDeNegocioException("Já existe uma conta com este e-mail.");
        if (await _usuarios.CpfExisteAsync(cpf)) throw new RegraDeNegocioException("Já existe uma conta com este CPF.");
        if (dto.DataNascimento.HasValue && dto.DataNascimento.Value.Date > DateTime.Today)
            throw new RegraDeNegocioException("A data de nascimento não pode ser no futuro.");

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            NomeSocial = string.IsNullOrWhiteSpace(dto.NomeSocial) ? null : dto.NomeSocial.Trim(),
            DataNascimento = dto.DataNascimento?.Date,
            Email = email,
            Cpf = cpf,
            Telefone = dto.Telefone?.Trim(),
            SenhaHash = _hasher.Gerar(dto.Senha),
            Perfil = Perfis.Cliente
        };
        _usuarios.Adicionar(usuario);
        await _uow.SalvarAsync();

        return GerarResposta(usuario);
    }

    /// <summary>
    /// A pessoa logada troca a própria senha (RF34). Também é o caminho obrigatório de quem recebeu
    /// uma senha provisória do administrador (RF35): depois disso, TrocarSenha volta a ser falso.
    /// </summary>
    public async Task<UsuarioDto> AlterarSenhaAsync(int idUsuario, AlterarSenhaDto dto)
    {
        var usuario = await _usuarios.ObterPorIdAsync(idUsuario) ?? throw new NaoEncontradoException("Conta não encontrada.");
        if (!_hasher.Verificar(dto.SenhaAtual, usuario.SenhaHash)) throw new RegraDeNegocioException("A senha atual está incorreta.");
        if (dto.NovaSenha == dto.SenhaAtual) throw new RegraDeNegocioException("A nova senha precisa ser diferente da atual.");

        usuario.DefinirNovaSenha(_hasher.Gerar(dto.NovaSenha));
        await _uow.SalvarAsync();
        return usuario.ParaDto();
    }

    public async Task<UsuarioDto> ObterAsync(int idUsuario)
    {
        var usuario = await _usuarios.ObterPorIdAsync(idUsuario)
                      ?? throw new NaoEncontradoException("Usuário não encontrado.");
        return usuario.ParaDto();
    }

    private LoginRespostaDto GerarResposta(Usuario usuario)
    {
        var (token, expira) = _tokens.Gerar(usuario);
        return new LoginRespostaDto(token, expira, usuario.ParaDto());
    }
}

/// <summary>Validação do CPF pelos dígitos verificadores (regra oficial da Receita Federal).</summary>
public static class ValidadorCpf
{
    public static string SomenteNumeros(string cpf) => new(cpf.Where(char.IsDigit).ToArray());

    public static string Formatar(string cpf)
    {
        var n = SomenteNumeros(cpf);
        return n.Length == 11 ? $"{n[..3]}.{n[3..6]}.{n[6..9]}-{n[9..]}" : cpf;
    }

    public static bool EhValido(string cpf)
    {
        var n = SomenteNumeros(cpf);
        if (n.Length != 11 || n.Distinct().Count() == 1) return false;

        int Digito(int quantidade)
        {
            var soma = 0;
            for (var i = 0; i < quantidade; i++) soma += (n[i] - '0') * (quantidade + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        return Digito(9) == n[9] - '0' && Digito(10) == n[10] - '0';
    }
}
