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
        if (!_sessao.Logado || CurrentState?.Location?.OriginalString?.Contains("login") != true) return;
        // Senha provisória ainda não trocada (RF35): primeiro a senha nova.
        if (_sessao.Usuario?.TrocarSenha == true) { await GoToAsync("alterar-senha?obrigatoria=true"); return; }
        // Quem já entrou antes vai direto para os produtos (ou para as entregas).
        await GoToAsync(_sessao.EhEntregador ? "//entregas" : "//catalogo");
    }
}
