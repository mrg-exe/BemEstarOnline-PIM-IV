using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Criar conta pelo app (RF36): mesmos campos e validações do site (cadastro.js). Usa POST /auth/registrar
/// e já entra logado.
/// </summary>
public partial class CriarContaViewModel : BaseViewModel
{
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly CarrinhoServico _carrinho;

    [ObservableProperty] private string nome = "";
    [ObservableProperty] private string nomeSocial = "";
    [ObservableProperty] private string nascimento = "";
    [ObservableProperty] private string cpf = "";
    [ObservableProperty] private string telefone = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string senha = "";
    [ObservableProperty] private string repetirSenha = "";
    [ObservableProperty] private bool concordo;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemErro))]
    private string erro = "";

    public bool TemErro => Erro.Length > 0;

    public CriarContaViewModel(ApiServico api, SessaoServico sessao, CarrinhoServico carrinho)
    {
        _api = api;
        _sessao = sessao;
        _carrinho = carrinho;
    }

    partial void OnCpfChanged(string value) => Cpf = Validacao.MascaraCpf(value);
    partial void OnNascimentoChanged(string value) => Nascimento = Validacao.MascaraData(value);
    partial void OnTelefoneChanged(string value) => Telefone = Validacao.MascaraTelefone(value);

    [RelayCommand]
    private void AlternarConcordo() => Concordo = !Concordo;

    // Textos da LGPD dentro do próprio app (RF41): funcionam sem internet e sem sair do cadastro.
    [RelayCommand]
    private Task VerPrivacidadeAsync() => Shell.Current.GoToAsync("texto-legal?doc=privacidade");

    [RelayCommand]
    private Task VerTermosAsync() => Shell.Current.GoToAsync("texto-legal?doc=termos");

    private string? PrimeiroErro()
    {
        if (Nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2) return "Digite nome e sobrenome.";
        if (!Validacao.CpfValido(Cpf)) return "CPF inválido. Confira os números.";
        if (!Validacao.EmailValido(Email)) return "Digite um e-mail válido.";
        if (Senha.Length < 6) return "A senha precisa ter pelo menos 6 caracteres.";
        if (RepetirSenha != Senha) return "As senhas não são iguais.";
        if (!string.IsNullOrWhiteSpace(Nascimento) && Validacao.Data(Nascimento) is null)
            return "Data inválida. Use dia/mês/ano, por exemplo 20/05/1998.";
        if (!Concordo) return "Para criar a conta, marque a concordância com o uso dos dados.";
        return null;
    }

    [RelayCommand]
    private async Task CriarAsync()
    {
        Erro = PrimeiroErro() ?? "";
        if (Erro.Length > 0) { await AvisarAsync(Erro); return; }

        Ocupado = true;
        try
        {
            var resposta = await _api.PostAsync<LoginResposta>("auth/registrar", new
            {
                nome = Nome.Trim(),
                nomeSocial = string.IsNullOrWhiteSpace(NomeSocial) ? null : NomeSocial.Trim(),
                dataNascimento = Validacao.Data(Nascimento)?.ToString("yyyy-MM-dd"),
                email = Email.Trim(),
                cpf = Cpf,
                telefone = string.IsNullOrWhiteSpace(Telefone) ? null : Telefone.Trim(),
                senha = Senha
            });
            _sessao.Entrar(resposta);
            Senha = RepetirSenha = "";
            await _carrinho.SincronizarAoEntrarAsync();
            await AvisarAsync($"Conta criada! Bem-vindo(a), {resposta.Usuario.NomeExibicao}.");
            await Shell.Current.GoToAsync("//catalogo");
        }
        catch (ApiException ex)
        {
            Erro = ex.SemConexao ? "Sem conexão com o servidor da farmácia. Ligue a API para criar a conta." : ex.Message;
            await AvisarAsync(Erro);
        }
        finally { Ocupado = false; }
    }
}
