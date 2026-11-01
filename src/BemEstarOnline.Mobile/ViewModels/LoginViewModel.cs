using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly CarrinhoServico _carrinho;
    private readonly SincronizacaoServico _sincronizacao;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string senha = "";
    [ObservableProperty] private string urlApi = "";
    [ObservableProperty] private bool mostrarServidor;

    public LoginViewModel(ApiServico api, SessaoServico sessao, CarrinhoServico carrinho, SincronizacaoServico sincronizacao)
    {
        _api = api;
        _sessao = sessao;
        _carrinho = carrinho;
        _sincronizacao = sincronizacao;
        UrlApi = sessao.UrlApi;
    }

    [RelayCommand]
    private async Task EntrarAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        {
            await AvisarAsync("Digite o e-mail e a senha.");
            return;
        }
        Ocupado = true;
        try
        {
            _sessao.UrlApi = UrlApi;
            var resposta = await _api.PostAsync<LoginResposta>("auth/login", new { email = Email.Trim(), senha = Senha });
            _sessao.Entrar(resposta);
            Senha = "";
            // Senha provisória dada pelo administrador (RF35): troca obrigatória antes de usar o app.
            if (resposta.Usuario.TrocarSenha)
            {
                await Shell.Current.GoToAsync("alterar-senha?obrigatoria=true");
                return;
            }
            if (resposta.Usuario.EhEntregador)
            {
                await Shell.Current.GoToAsync("//entregas");
                return;
            }
            await _carrinho.SincronizarAoEntrarAsync();
            _ = _sincronizacao.SincronizarAsync();
            await Shell.Current.GoToAsync("//catalogo");
        }
        catch (ApiException ex)
        {
            await AvisarAsync(ex.SemConexao
                ? $"Não consegui falar com a API em {UrlApi}. Confira se ela está ligada e se o endereço do servidor está certo."
                : ex.Message);
        }
        finally { Ocupado = false; }
    }

    /// <summary>
    /// Esqueci minha senha (RF33): pede o e-mail e a API manda o link (o mesmo do site). A resposta é
    /// sempre igual, exista ou não a conta. O link abre a página do site para criar a senha nova.
    /// </summary>
    [RelayCommand]
    private async Task EsqueciSenhaAsync()
    {
        var email = await Shell.Current.DisplayPromptAsync("Esqueci minha senha",
            "Digite o e-mail da sua conta. Vamos mandar um link para criar uma senha nova (vale 30 minutos).",
            "Enviar link", "Cancelar", "seu@email.com", keyboard: Keyboard.Email, initialValue: Email.Trim());
        if (string.IsNullOrWhiteSpace(email)) return;
        Ocupado = true;
        try
        {
            _sessao.UrlApi = UrlApi;
            var r = await _api.PostAsync<RespostaMensagem>("auth/esqueci-senha", new { email = email.Trim() });
            await AvisarAsync(r.Mensagem);
        }
        catch (ApiException ex) { await AvisarAsync(ex.SemConexao ? "Sem conexão com o servidor da farmácia." : ex.Message); }
        finally { Ocupado = false; }
    }

    /// <summary>Criar conta pelo próprio app (RF36). Usa o endereço do servidor digitado aqui.</summary>
    [RelayCommand]
    private Task CriarContaAsync()
    {
        _sessao.UrlApi = UrlApi;
        return Shell.Current.GoToAsync("criar-conta");
    }

    [RelayCommand]
    private Task ContinuarSemEntrarAsync() => Shell.Current.GoToAsync("//catalogo");

    [RelayCommand]
    private void AlternarServidor() => MostrarServidor = !MostrarServidor;

    /// <summary>
    /// Contas de teste para a apresentação: um toque preenche e entra. O cliente vai para os produtos
    /// e o entregador para a lista de entregas (o app muda de modo sozinho pelo perfil da conta).
    /// </summary>
    [RelayCommand]
    private Task EntrarComoAsync(string perfil)
    {
        (Email, Senha) = perfil == "entregador"
            ? ("entregador@bemestaronline.com", "Entregador@123")
            : ("cliente@bemestaronline.com", "Cliente@123");
        return EntrarAsync();
    }
}
