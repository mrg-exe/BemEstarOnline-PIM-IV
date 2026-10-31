using BemEstarOnline.Mobile.Modelos;

namespace BemEstarOnline.Mobile.Servicos;

/// <summary>Sair da conta e excluir a conta, apagando do celular tudo o que era dela.</summary>
public class ContaServico
{
    private readonly SessaoServico _sessao;
    private readonly BancoLocalServico _banco;
    private readonly Armazenamento _armazenamento;
    private readonly ApiServico _api;

    public ContaServico(SessaoServico sessao, BancoLocalServico banco, Armazenamento armazenamento, ApiServico api)
    {
        _sessao = sessao;
        _banco = banco;
        _armazenamento = armazenamento;
        _api = api;
    }

    /// <summary>Sai e limpa carrinho e endereços guardados (o próximo usuário não vê nada da conta anterior).</summary>
    public async Task SairAsync()
    {
        _sessao.Sair();
        _armazenamento.Gravar("enderecos", null);
        await _banco.LimparCarrinhoAsync();
    }

    public async Task ExcluirContaAsync(string senha)
    {
        await _api.PostAsync<object>("auth/excluir-conta", new { senha });
        await SairAsync();
    }
}
