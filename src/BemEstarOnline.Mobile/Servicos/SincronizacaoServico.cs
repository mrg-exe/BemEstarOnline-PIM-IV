using System.Text.Json;
using BemEstarOnline.Mobile.Modelos;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// Envia para a API os pedidos que foram feitos sem internet. Roda ao abrir o app,
/// quando a internet volta (Connectivity.ConnectivityChanged) e no botão "Sincronizar agora".
/// O servidor confere de novo preço e estoque, então nada é vendido sem estoque.
/// </summary>
public class SincronizacaoServico
{
    private readonly BancoLocalServico _banco;
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private bool _rodando;

    public SincronizacaoServico(BancoLocalServico banco, ApiServico api, SessaoServico sessao)
    {
        _banco = banco;
        _api = api;
        _sessao = sessao;
        Connectivity.Current.ConnectivityChanged += async (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet) await SincronizarAsync();
        };
    }

    public async Task<(int enviados, int comErro)> SincronizarAsync()
    {
        if (_rodando || !_sessao.Logado) return (0, 0);
        _rodando = true;
        var enviados = 0;
        var comErro = 0;
        try
        {
            if (!await _api.OnlineAsync()) return (0, 0);
            foreach (var pendente in await _banco.PendentesAsync(_sessao.Usuario?.IdUsuario ?? 0))
            {
                try
                {
                    var pedido = JsonSerializer.Deserialize<PedidoCriar>(pendente.JsonPedido)!;
                    var criado = await _api.PostAsync<PedidoDetalhe>("pedidos", pedido);
                    pendente.Sincronizado = true;
                    pendente.IdPedidoServidor = criado.IdPedido;
                    pendente.UltimoErro = null;
                    enviados++;
                }
                catch (ApiException ex) when (!ex.SemConexao)
                {
                    // Recusado pela API (ex.: estoque acabou). Marcamos para não tentar para sempre.
                    pendente.Sincronizado = true;
                    pendente.UltimoErro = ex.Message;
                    comErro++;
                }
                catch (ApiException ex)
                {
                    pendente.UltimoErro = ex.Message;
                    break;
                }
                await _banco.AtualizarPendenteAsync(pendente);
            }
        }
        finally { _rodando = false; }
        return (enviados, comErro);
    }

    public async Task<int> QuantidadePendenteAsync() => (await _banco.PendentesAsync(_sessao.Usuario?.IdUsuario ?? 0)).Count;
}
