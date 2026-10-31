using System.Collections.ObjectModel;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

public partial class PedidosViewModel : BaseViewModel
{
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly SincronizacaoServico _sincronizacao;

    public ObservableCollection<PedidoResumo> Pedidos { get; } = new();

    [ObservableProperty] private string aviso = "";
    [ObservableProperty] private bool temAviso;
    [ObservableProperty] private bool atualizando;
    [ObservableProperty] private bool precisaLogin;

    public PedidosViewModel(ApiServico api, SessaoServico sessao, SincronizacaoServico sincronizacao)
    {
        _api = api;
        _sessao = sessao;
        _sincronizacao = sincronizacao;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        PrecisaLogin = !_sessao.Logado;
        if (PrecisaLogin) { Atualizando = false; return; }
        Ocupado = true;
        try
        {
            var (enviados, comErro) = await _sincronizacao.SincronizarAsync();
            var pendentes = await _sincronizacao.QuantidadePendenteAsync();

            var lista = await _api.GetAsync<List<PedidoResumo>>("pedidos");
            Pedidos.Clear();
            foreach (var p in lista) Pedidos.Add(p);

            var partes = new List<string>();
            if (enviados > 0) partes.Add($"{enviados} pedido(s) feitos offline foram enviados.");
            if (comErro > 0) partes.Add($"{comErro} pedido(s) offline foram recusados (ex.: estoque acabou).");
            if (pendentes > 0) partes.Add($"{pendentes} pedido(s) aguardando internet para envio.");
            Aviso = string.Join(" ", partes);
            TemAviso = partes.Count > 0;
        }
        catch (ApiException ex)
        {
            var pendentes = await _sincronizacao.QuantidadePendenteAsync();
            Aviso = ex.SemConexao
                ? $"Sem conexão. {(pendentes > 0 ? $"{pendentes} pedido(s) aguardando envio." : "")}"
                : ex.Message;
            TemAviso = true;
        }
        finally
        {
            Ocupado = false;
            Atualizando = false;
        }
    }

    [RelayCommand]
    private Task AbrirAsync(PedidoResumo pedido) => Shell.Current.GoToAsync($"pedido?id={pedido.IdPedido}");

    [RelayCommand]
    private Task EntrarAsync() => Shell.Current.GoToAsync("//login");
}
