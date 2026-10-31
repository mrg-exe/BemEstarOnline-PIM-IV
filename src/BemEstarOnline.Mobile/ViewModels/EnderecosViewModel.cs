using System.Collections.ObjectModel;
using System.Text.Json;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Meus endereços (RF37): listar, adicionar e remover, igual a "Minha conta > Endereços" do site.
/// O CEP preenche rua, bairro, cidade e estado (ViaCEP); "Usar minha localização" grava o GPS do celular.
/// Aberta pelo carrinho ("Novo endereço"), volta para ele depois de salvar.
/// </summary>
public partial class EnderecosViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ApiServico _api;
    private readonly LocalizacaoServico _localizacao;
    private readonly Armazenamento _armazenamento;
    private (double Latitude, double Longitude)? _gps;

    public ObservableCollection<Endereco> Enderecos { get; } = new();

    [ObservableProperty] private bool vazio;
    [ObservableProperty] private bool doCarrinho;
    [ObservableProperty] private string apelido = "";
    [ObservableProperty] private string cep = "";
    [ObservableProperty] private string logradouro = "";
    [ObservableProperty] private string numero = "";
    [ObservableProperty] private string complemento = "";
    [ObservableProperty] private string bairro = "";
    [ObservableProperty] private string cidade = "";
    [ObservableProperty] private string estado = "";
    [ObservableProperty] private bool principal;
    [ObservableProperty] private string situacaoCep = "";
    [ObservableProperty] private string situacaoGps = "";

    public EnderecosViewModel(ApiServico api, LocalizacaoServico localizacao, Armazenamento armazenamento)
    {
        _api = api;
        _localizacao = localizacao;
        _armazenamento = armazenamento;
        WeakReferenceMessenger.Default.Register<EnderecosViewModel, EnderecosMudaram>(this, (vm, m) =>
        {
            if (m.Origem != vm) _ = vm.CarregarAsync();
        });
    }

    public bool TemSituacaoCep => SituacaoCep.Length > 0;
    public bool TemSituacaoGps => SituacaoGps.Length > 0;
    partial void OnSituacaoCepChanged(string value) => OnPropertyChanged(nameof(TemSituacaoCep));
    partial void OnSituacaoGpsChanged(string value) => OnPropertyChanged(nameof(TemSituacaoGps));

    public void ApplyQueryAttributes(IDictionary<string, object> query) => DoCarrinho = query.ContainsKey("carrinho");

    [RelayCommand]
    public async Task CarregarAsync()
    {
        try
        {
            var lista = await _api.GetAsync<List<Endereco>>("enderecos");
            _armazenamento.Gravar("enderecos", JsonSerializer.Serialize(lista)); // o carrinho usa esta cópia sem internet
            Enderecos.Clear();
            foreach (var e in lista) Enderecos.Add(e);
            // O primeiro endereço já nasce como principal.
            if (lista.Count == 0) Principal = true;
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        Vazio = Enderecos.Count == 0;
    }

    partial void OnCepChanged(string value)
    {
        var formatado = Validacao.MascaraCep(value);
        if (formatado != value) { Cep = formatado; return; }
        if (Validacao.SoNumeros(value).Length == 8) _ = PreencherPorCepAsync(value);
    }

    private async Task PreencherPorCepAsync(string valor)
    {
        SituacaoCep = "Procurando o CEP...";
        var r = await _localizacao.BuscarCepAsync(valor);
        if (r is null) { SituacaoCep = "CEP não encontrado. Preencha o endereço à mão."; return; }
        if (string.IsNullOrWhiteSpace(Logradouro)) Logradouro = r.Logradouro;
        if (r.Bairro.Length > 0) Bairro = r.Bairro;
        if (r.Cidade.Length > 0) Cidade = r.Cidade;
        if (r.Estado.Length > 0) Estado = r.Estado;
        SituacaoCep = "Endereço preenchido pelo CEP. Agora é só o número.";
    }

    [RelayCommand]
    private async Task UsarLocalizacaoAsync()
    {
        SituacaoGps = "Procurando...";
        _gps = await _localizacao.MinhaPosicaoAsync();
        SituacaoGps = _gps is null
            ? "Não deu para usar a localização; usaremos o endereço."
            : $"Localização registrada ({_gps.Value.Latitude:0.0000}, {_gps.Value.Longitude:0.0000}).";
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        var faltando = new (string Valor, string Nome)[]
        {
            (Cep, "o CEP"), (Logradouro, "a rua"), (Numero, "o número"), (Bairro, "o bairro"), (Cidade, "a cidade"), (Estado, "o estado")
        }.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Valor));
        if (faltando.Nome is not null) { await AvisarAsync($"Preencha {faltando.Nome}."); return; }
        if (Estado.Trim().Length != 2) { await AvisarAsync("Use a sigla do estado (ex.: SP)."); return; }

        Ocupado = true;
        try
        {
            // Sem GPS, tenta achar o ponto no mapa pelo endereço (para o mapa da entrega).
            var posicao = _gps ?? await _localizacao.CoordenadasDoEnderecoAsync(Numero.Trim(), Logradouro.Trim(), Cidade.Trim(), Estado.Trim());
            await _api.PostAsync<Endereco>("enderecos", new
            {
                apelido = string.IsNullOrWhiteSpace(Apelido) ? null : Apelido.Trim(),
                cep = Cep.Trim(),
                logradouro = Logradouro.Trim(),
                numero = Numero.Trim(),
                complemento = string.IsNullOrWhiteSpace(Complemento) ? null : Complemento.Trim(),
                bairro = Bairro.Trim(),
                cidade = Cidade.Trim(),
                estado = Estado.Trim().ToUpperInvariant(),
                principal = Principal,
                latitude = posicao?.Latitude,
                longitude = posicao?.Longitude
            });
            Apelido = Cep = Logradouro = Numero = Complemento = Bairro = Cidade = Estado = SituacaoCep = SituacaoGps = "";
            Principal = false;
            _gps = null;
            await CarregarAsync();
            WeakReferenceMessenger.Default.Send(new EnderecosMudaram(this));
            await AvisarAsync("Endereço salvo.");
            if (DoCarrinho) await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private async Task RemoverAsync(Endereco endereco)
    {
        if (!await ConfirmarAsync($"Remover o endereço \"{endereco.Titulo}\"?")) return;
        try
        {
            await _api.DeleteAsync($"enderecos/{endereco.IdEndereco}");
            await CarregarAsync();
            WeakReferenceMessenger.Default.Send(new EnderecosMudaram(this));
        }
        catch (ApiException ex) { await AvisarAsync(ex.Message); }
    }
}
