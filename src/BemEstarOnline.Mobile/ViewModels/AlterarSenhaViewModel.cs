using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Alterar senha (RF34). Também é a tela obrigatória do primeiro acesso de quem recebeu
/// senha provisória do administrador (RF35): sem trocar, não dá para usar o app.
/// </summary>
public partial class AlterarSenhaViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly CarrinhoServico _carrinho;
    private readonly ContaServico _conta;

    [ObservableProperty] private bool obrigatoria;
    [ObservableProperty] private string senhaAtual = "";
    [ObservableProperty] private string novaSenha = "";
    [ObservableProperty] private string repetirSenha = "";

    public string RotuloSenhaAtual => Obrigatoria ? "Senha provisória (a que o administrador passou)" : "Senha atual";
    public bool PodeVoltar => !Obrigatoria;

    public AlterarSenhaViewModel(ApiServico api, SessaoServico sessao, CarrinhoServico carrinho, ContaServico conta)
    {
        _api = api;
        _sessao = sessao;
        _carrinho = carrinho;
        _conta = conta;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Obrigatoria = _sessao.Usuario?.TrocarSenha == true
                      || (query.TryGetValue("obrigatoria", out var o) && o?.ToString() == "true");
        OnPropertyChanged(nameof(RotuloSenhaAtual));
        OnPropertyChanged(nameof(PodeVoltar));
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrEmpty(SenhaAtual)) { await AvisarAsync("Digite a senha atual."); return; }
        if (NovaSenha.Length < 6) { await AvisarAsync("A nova senha precisa ter pelo menos 6 caracteres."); return; }
        if (NovaSenha != RepetirSenha) { await AvisarAsync("As duas senhas novas não são iguais."); return; }

        Ocupado = true;
        try
        {
            var usuario = await _api.PostAsync<Usuario>("auth/alterar-senha", new { senhaAtual = SenhaAtual, novaSenha = NovaSenha });
            _sessao.AtualizarUsuario(usuario);
            SenhaAtual = NovaSenha = RepetirSenha = "";
            await AvisarAsync("Senha alterada!");

            if (!Obrigatoria) { await Shell.Current.GoToAsync(".."); return; }
            // Primeiro acesso concluído: segue para a tela de cada perfil.
            if (usuario.EhEntregador) { await Shell.Current.GoToAsync("//entregas"); return; }
            await _carrinho.SincronizarAoEntrarAsync();
            await Shell.Current.GoToAsync("//catalogo");
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        finally { Ocupado = false; }
    }

    /// <summary>Na troca obrigatória não há "voltar": ou cria a senha, ou sai da conta.</summary>
    [RelayCommand]
    private async Task SairAsync()
    {
        await _conta.SairAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
