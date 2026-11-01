using BemEstarOnline.Mobile.Servicos;
using BemEstarOnline.Mobile.Views;

namespace BemEstarOnline.Mobile;

public partial class AppShell : Shell
{
    private readonly SessaoServico _sessao;

    public AppShell(SessaoServico sessao)
    {
        InitializeComponent();
        _sessao = sessao;
        // Telas abertas "por cima" das abas
        Routing.RegisterRoute("produto", typeof(ProdutoPage));
        Routing.RegisterRoute("pedido", typeof(PedidoDetalhePage));
        Routing.RegisterRoute("entrega", typeof(EntregaDetalhePage));
        Routing.RegisterRoute("alterar-senha", typeof(AlterarSenhaPage));
        Routing.RegisterRoute("criar-conta", typeof(CriarContaPage));
        Routing.RegisterRoute("enderecos", typeof(EnderecosPage));
        Routing.RegisterRoute("assinaturas", typeof(AssinaturasPage));
        Routing.RegisterRoute("enviar-receita", typeof(EnviarReceitaPage));
        Routing.RegisterRoute("texto-legal", typeof(TextoLegalPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // O app fechou sozinho da última vez? Mostra o erro (para copiar e mandar) e NÃO entra direto nos produtos,
        // senão ele fecharia de novo no mesmo ponto, sem parar.
        if (RegistroDeErros.LerEApagar() is { } erro)
        {
            var copiar = await DisplayAlertAsync("O app teve um problema",
                "Da última vez o app fechou sozinho ou algo deu errado. Toque em \"Copiar erro\" e mande o texto para quem " +
                "cuida do sistema.\n\n" + (erro.Length > 900 ? erro[..900] + "..." : erro), "Copiar erro", "Fechar");
            if (copiar) await Clipboard.Default.SetTextAsync(erro);
            return;
        }
        if (!_sessao.Logado || CurrentState?.Location?.OriginalString?.Contains("login") != true) return;
        // Senha provisória ainda não trocada (RF35): primeiro a senha nova.
        if (_sessao.Usuario?.TrocarSenha == true) { await GoToAsync("alterar-senha?obrigatoria=true"); return; }
        // Quem já entrou antes vai direto para os produtos (ou para as entregas).
        await GoToAsync(_sessao.EhEntregador ? "//entregas" : "//catalogo");
    }

    private bool _perguntandoSeSai;

    // Botão "voltar" do Android. Nas telas de dentro (produto, entrega...) ele volta normalmente. Nas telas principais
    // (abas e login) não há nada atrás, então o Android fecharia o app: antes disso, pergunta se a pessoa quer sair.
    protected override bool OnBackButtonPressed()
    {
        if (Navigation.NavigationStack.Count > 1 || Navigation.ModalStack.Count > 0)
            return base.OnBackButtonPressed();
        if (!_perguntandoSeSai) _ = PerguntarSeSaiAsync();
        return true;
    }

    private async Task PerguntarSeSaiAsync()
    {
        _perguntandoSeSai = true;
        try
        {
            if (!await DisplayAlertAsync("Sair do app?", "Deseja sair do Bem Estar Online? Sua conta continua conectada.",
                    "Sair", "Ficar")) return;
#if ANDROID
            // Mesmo efeito do "voltar" normal do Android: o app vai para o fundo, sem perder a sessão.
            Platform.CurrentActivity?.MoveTaskToBack(true);
#endif
        }
        finally { _perguntandoSeSai = false; }
    }
}
