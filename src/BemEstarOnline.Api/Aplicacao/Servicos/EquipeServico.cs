using BemEstarOnline.Api.Aplicacao.Dtos;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Dominio;
using BemEstarOnline.Api.Dominio.Entidades;
using BemEstarOnline.Api.Dominio.Interfaces;

namespace BemEstarOnline.Api.Aplicacao.Servicos;

/// <summary>
/// Gestão da equipe pelo administrador (RF35): cadastrar atendente, entregador ou administrador,
/// mudar o perfil, desativar/reativar a conta e redefinir a senha (sempre provisória).
/// Regras: ninguém se desativa nem muda o próprio perfil; a farmácia nunca fica sem um ADMIN ativo;
/// cliente não vira equipe por aqui.
/// </summary>
public class EquipeServico : IEquipeServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly ISenhaHasher _hasher;
    private readonly IUnidadeDeTrabalho _uow;

    public EquipeServico(IUsuarioRepositorio usuarios, ISenhaHasher hasher, IUnidadeDeTrabalho uow)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _uow = uow;
    }

    public async Task<List<EquipeDto>> ListarAsync() =>
        (await _usuarios.ListarAsync())
            .Where(u => u.Perfil != Perfis.Cliente)
            .OrderByDescending(u => u.Ativo).ThenBy(u => u.Nome)
            .Select(u => u.ParaEquipeDto())
            .ToList();

    public async Task<EquipeDto> CriarAsync(EquipeCriarDto dto)
    {
        var perfil = ValidarPerfil(dto.Perfil);
        var email = dto.Email.Trim().ToLowerInvariant();
        var cpf = ValidadorCpf.Formatar(dto.Cpf);

        if (!ValidadorCpf.EhValido(cpf)) throw new RegraDeNegocioException("CPF inválido. Confira os números.");
        if (await _usuarios.EmailExisteAsync(email)) throw new RegraDeNegocioException("Já existe uma conta com este e-mail.");
        if (await _usuarios.CpfExisteAsync(cpf)) throw new RegraDeNegocioException("Já existe uma conta com este CPF.");

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Cpf = cpf,
            Telefone = string.IsNullOrWhiteSpace(dto.Celular) ? null : dto.Celular.Trim(),
            Perfil = perfil
        };
        usuario.DefinirSenhaProvisoria(_hasher.Gerar(dto.SenhaInicial));
        _usuarios.Adicionar(usuario);
        await _uow.SalvarAsync();
        return usuario.ParaEquipeDto();
    }

    public async Task<EquipeDto> AtualizarAsync(int idAdmin, int idUsuario, EquipeAtualizarDto dto)
    {
        var usuario = await ObterDaEquipeAsync(idUsuario);
        var novoPerfil = dto.Perfil is null ? usuario.Perfil : ValidarPerfil(dto.Perfil);
        var novoAtivo = dto.Ativo ?? usuario.Ativo;

        if (idUsuario == idAdmin && (novoPerfil != usuario.Perfil || !novoAtivo))
            throw new RegraDeNegocioException("Você não pode mudar o próprio perfil nem desativar a própria conta. Peça a outro administrador.");

        // A farmácia nunca pode ficar sem um administrador ativo.
        var deixaDeSerAdminAtivo = usuario.Perfil == Perfis.Admin && usuario.Ativo && (novoPerfil != Perfis.Admin || !novoAtivo);
        if (deixaDeSerAdminAtivo && await ContarAdminsAtivosAsync() <= 1)
            throw new RegraDeNegocioException("Este é o último administrador ativo. Cadastre outro administrador antes.");

        usuario.Perfil = novoPerfil;
        usuario.Ativo = novoAtivo;
        usuario.AtualizadoEm = DateTime.Now;
        await _uow.SalvarAsync();
        return usuario.ParaEquipeDto();
    }

    public async Task<EquipeDto> RedefinirSenhaAsync(int idAdmin, int idUsuario, string novaSenha)
    {
        if (idUsuario == idAdmin)
            throw new RegraDeNegocioException("Para trocar a sua própria senha, use \"Alterar senha\" em Minha conta.");
        var usuario = await ObterDaEquipeAsync(idUsuario);
        usuario.DefinirSenhaProvisoria(_hasher.Gerar(novaSenha));
        await _uow.SalvarAsync();
        return usuario.ParaEquipeDto();
    }

    private async Task<Usuario> ObterDaEquipeAsync(int idUsuario)
    {
        var usuario = await _usuarios.ObterPorIdAsync(idUsuario) ?? throw new NaoEncontradoException("Pessoa não encontrada.");
        if (usuario.Perfil == Perfis.Cliente)
            throw new RegraDeNegocioException("Esta conta é de cliente. A gestão da equipe só altera contas de funcionários.");
        return usuario;
    }

    private static string ValidarPerfil(string perfil)
    {
        perfil = (perfil ?? "").Trim().ToUpperInvariant();
        if (!Perfis.Funcionarios.Contains(perfil))
            throw new RegraDeNegocioException("Perfil inválido. Escolha atendente, entregador ou administrador.");
        return perfil;
    }

    private async Task<int> ContarAdminsAtivosAsync() =>
        (await _usuarios.ListarAsync()).Count(u => u.Perfil == Perfis.Admin && u.Ativo);
}
