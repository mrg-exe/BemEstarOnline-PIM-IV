using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

public partial class PerfilViewModel : BaseViewModel
{
    private readonly SessaoServico _sessao;
    private readonly AcessibilidadeServico _acessibilidade;
    private readonly SincronizacaoServico _sincronizacao;
    private readonly ContaServico _conta;
    private bool _carregando;

    [ObservableProperty] private string nome = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private bool logado;
    [ObservableProperty] private bool altoContraste;
    [ObservableProperty] private bool letraGrande;
    [ObservableProperty] private string servidor = "";

    /// <summary>Só clientes: excluir a conta, endereços e assinaturas (a equipe não usa essas telas).</summary>
    [ObservableProperty] private bool podeExcluir;

    public PerfilViewModel(SessaoServico sessao, AcessibilidadeServico acessibilidade, SincronizacaoServico sincronizacao, ContaServico conta)
    {
        _conta = conta;
        _sessao = sessao;
        _acessibilidade = acessibilidade;
        _sincronizacao = sincronizacao;
    }

    public void Carregar()
    {
        _carregando = true;
        var u = _sessao.Usuario;
        Logado = _sessao.Logado;
        Nome = u?.NomeExibicao ?? "Visitante";
        Email = u?.Email ?? "Entre para comprar e acompanhar pedidos";
        AltoContraste = _acessibilidade.AltoContraste;
        LetraGrande = _acessibilidade.LetraGrande;
        Servidor = _sessao.UrlApi;
        PodeExcluir = Logado && u?.Perfil == "CLIENTE";
        _carregando = false;
    }

    partial void OnAltoContrasteChanged(bool value) { if (!_carregando) _acessibilidade.Definir(value, LetraGrande); }
    partial void OnLetraGrandeChanged(bool value) { if (!_carregando) _acessibilidade.Definir(AltoContraste, value); }

    [RelayCommand]
    private async Task SincronizarAsync()
    {
        var (enviados, comErro) = await _sincronizacao.SincronizarAsync();
        var pendentes = await _sincronizacao.QuantidadePendenteAsync();
        await AvisarAsync($"Enviados: {enviados}. Recusados: {comErro}. Ainda aguardando internet: {pendentes}.");
    }

    /// <summary>Alterar a própria senha (RF34).</summary>
    [RelayCommand]
    private Task AlterarSenhaAsync() => Shell.Current.GoToAsync("alterar-senha");

    [RelayCommand]
    private Task AbrirEnderecosAsync() => Shell.Current.GoToAsync("enderecos");

    [RelayCommand]
    private Task AbrirAssinaturasAsync() => Shell.Current.GoToAsync("assinaturas");

    [RelayCommand]
    private Task EnviarReceitaAsync() => Shell.Current.GoToAsync("enviar-receita");

    /// <summary>Política de Privacidade e Termos de Uso (RF41), para todos (até quem não entrou).</summary>
    [RelayCommand]
    private Task VerPrivacidadeAsync() => Shell.Current.GoToAsync("texto-legal?doc=privacidade");

    [RelayCommand]
    private Task VerTermosAsync() => Shell.Current.GoToAsync("texto-legal?doc=termos");

    [RelayCommand]
    private async Task SairOuEntrarAsync()
    {
        if (_sessao.Logado) await _conta.SairAsync(); // limpa carrinho e endereços deste aparelho
        await Shell.Current.GoToAsync("//login");
    }

    /// <summary>Exclusão da conta pelo próprio cliente (LGPD), sem precisar falar com o suporte.</summary>
    [RelayCommand]
    private async Task ExcluirContaAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync("Excluir conta",
            "Seus dados pessoais, endereços, carrinho e receitas enviadas serão apagados e as assinaturas canceladas. " +
            "Os pedidos antigos ficam guardados pela farmácia sem seus dados (exigência fiscal). Continuar?", "Excluir", "Voltar");
        if (!ok) return;
        var senha = await Shell.Current.DisplayPromptAsync("Confirmar", "Digite sua senha para excluir a conta:", "Excluir", "Cancelar");
        if (string.IsNullOrEmpty(senha)) return;
        try
        {
            await _conta.ExcluirContaAsync(senha);
            await AvisarAsync("Sua conta foi excluída.");
            await Shell.Current.GoToAsync("//login");
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }
}
