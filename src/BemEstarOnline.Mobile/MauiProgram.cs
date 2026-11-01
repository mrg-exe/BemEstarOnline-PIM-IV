using BemEstarOnline.Mobile.Servicos;
using BemEstarOnline.Mobile.ViewModels;
using BemEstarOnline.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace BemEstarOnline.Mobile;

/// <summary>Configuração do app: registra serviços, ViewModels e telas (injeção de dependência).</summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        RegistroDeErros.Iniciar(); // guarda a mensagem se o app fechar sozinho (aparece na próxima abertura)
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if ANDROID
        // Campos de texto sem o "EmojiCompat" do Android: nenhum campo do app precisa de emoji, e ele fechava o app
        // quando a máscara (CPF, data, celular, CEP) mudava o texto junto com a tecla.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SemEmojiCompat", (h, _) => h.PlatformView.EmojiCompatEnabled = false);
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("SemEmojiCompat", (h, _) => h.PlatformView.EmojiCompatEnabled = false);
#endif

#if WINDOWS
        // No Windows a caixa de marcar reserva 120 px de largura; aqui ela fica do tamanho do quadradinho
        // (o texto ao lado é um Label, como no consentimento da LGPD em "Criar conta").
        Microsoft.Maui.Handlers.CheckBoxHandler.Mapper.AppendToMapping("SemLarguraMinima", (h, _) => h.PlatformView.MinWidth = 0);
#endif

        // Serviços (uma instância para o app inteiro)
        builder.Services.AddSingleton<Armazenamento>();
        builder.Services.AddSingleton<SessaoServico>();
        builder.Services.AddSingleton<ApiServico>();
        builder.Services.AddSingleton<BancoLocalServico>();
        builder.Services.AddSingleton<CarrinhoServico>();
        builder.Services.AddSingleton<SincronizacaoServico>();
        builder.Services.AddSingleton<AcessibilidadeServico>();
        builder.Services.AddSingleton<ContaServico>();
        builder.Services.AddSingleton<LocalizacaoServico>();
        builder.Services.AddSingleton<AppShell>();

        // ViewModels e telas
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<CatalogoViewModel>();
        builder.Services.AddTransient<ProdutoViewModel>();
        builder.Services.AddTransient<CarrinhoViewModel>();
        builder.Services.AddTransient<PedidosViewModel>();
        builder.Services.AddTransient<PedidoDetalheViewModel>();
        builder.Services.AddTransient<PerfilViewModel>();
        builder.Services.AddTransient<EntregasViewModel>();
        builder.Services.AddTransient<EntregaDetalheViewModel>();
        builder.Services.AddTransient<AlterarSenhaViewModel>();
        builder.Services.AddTransient<CriarContaViewModel>();
        builder.Services.AddTransient<EnderecosViewModel>();
        builder.Services.AddTransient<AssinaturasViewModel>();
        builder.Services.AddTransient<EnviarReceitaViewModel>();
        builder.Services.AddTransient<TextoLegalViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CatalogoPage>();
        builder.Services.AddTransient<ProdutoPage>();
        builder.Services.AddTransient<CarrinhoPage>();
        builder.Services.AddTransient<PedidosPage>();
        builder.Services.AddTransient<PedidoDetalhePage>();
        builder.Services.AddTransient<PerfilPage>();
        builder.Services.AddTransient<EntregasPage>();
        builder.Services.AddTransient<EntregaDetalhePage>();
        builder.Services.AddTransient<AlterarSenhaPage>();
        builder.Services.AddTransient<CriarContaPage>();
        builder.Services.AddTransient<EnderecosPage>();
        builder.Services.AddTransient<AssinaturasPage>();
        builder.Services.AddTransient<EnviarReceitaPage>();
        builder.Services.AddTransient<TextoLegalPage>();

        return builder.Build();
    }
}
