using System.Net.Http.Headers;
using System.Text.Json;
using BemEstarOnline.Mobile.Modelos;
using BemEstarOnline.Mobile.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BemEstarOnline.Mobile.ViewModels;

/// <summary>
/// Enviar receita (RF39): a pessoa tira a foto (ou escolhe da galeria), vê a prévia e manda para a farmácia
/// com o que precisa. Usa a mesma rota do site (POST /api/delivery, formulário com arquivo). Não exige login.
/// A foto fica só num arquivo temporário do app e é apagada depois do envio.
/// </summary>
public partial class EnviarReceitaViewModel : BaseViewModel
{
    private const long TamanhoMaximo = 5 * 1024 * 1024;
    private readonly ApiServico _api;
    private readonly SessaoServico _sessao;
    private readonly Armazenamento _armazenamento;
    private string? _arquivoFoto;

    [ObservableProperty] private string nome = "";
    [ObservableProperty] private string telefone = "";
    [ObservableProperty] private string endereco = "";
    [ObservableProperty] private string pedido = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SemFoto))]
    private bool temFoto;

    [ObservableProperty] private ImageSource? previa;
    [ObservableProperty] private string tamanhoFoto = "";

    public bool SemFoto => !TemFoto;

    public EnviarReceitaViewModel(ApiServico api, SessaoServico sessao, Armazenamento armazenamento)
    {
        _api = api;
        _sessao = sessao;
        _armazenamento = armazenamento;
        // Tela nova: apaga fotos de receita que ficaram de uma vez anterior sem enviar (dado de saúde não fica largado).
        try
        {
            foreach (var antiga in Directory.EnumerateFiles(FileSystem.CacheDirectory, "receita-*")) File.Delete(antiga);
        }
        catch (IOException) { /* arquivo em uso: fica para a próxima */ }
    }

    partial void OnTelefoneChanged(string value)
    {
        var f = Validacao.MascaraTelefone(value);
        if (f != value) FormatarDepois(() => { if (Telefone == value) Telefone = f; });
    }

    /// <summary>Quem está logado já vem com nome, celular e endereço principal preenchidos.</summary>
    public async Task CarregarAsync()
    {
        var u = _sessao.Usuario;
        if (u is null) return;
        if (string.IsNullOrWhiteSpace(Nome)) Nome = u.NomeExibicao;
        if (string.IsNullOrWhiteSpace(Telefone) && !string.IsNullOrWhiteSpace(u.Telefone)) Telefone = u.Telefone;
        if (!string.IsNullOrWhiteSpace(Endereco)) return;

        // Endereço principal: da API (e guarda a cópia); sem internet, a cópia que já estava no celular.
        List<Endereco> lista;
        try
        {
            lista = await _api.GetAsync<List<Endereco>>("enderecos");
            _armazenamento.Gravar("enderecos", JsonSerializer.Serialize(lista));
        }
        catch (ApiException)
        {
            var salvo = _armazenamento.Ler("enderecos");
            lista = salvo is null ? new() : JsonSerializer.Deserialize<List<Endereco>>(salvo) ?? new();
        }
        if (string.IsNullOrWhiteSpace(Endereco))
            Endereco = (lista.FirstOrDefault(e => e.Principal) ?? lista.FirstOrDefault())?.TextoCompleto ?? "";
    }

    // Foto menor (até 2000 px) e sem os dados escondidos da imagem (como a localização GPS): mais leve e mais privada.
    private static MediaPickerOptions Opcoes => new()
    {
        Title = "Foto da receita",
        MaximumWidth = 2000,
        MaximumHeight = 2000,
        CompressionQuality = 85,
        RotateImage = true,
        PreserveMetaData = false
    };

    [RelayCommand]
    private async Task TirarFotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await AvisarAsync("Este aparelho não tem câmera disponível. Use \"Escolher da galeria\".");
            return;
        }
        try { await UsarFotoAsync(await MediaPicker.Default.CapturePhotoAsync(Opcoes)); }
        catch (PermissionException) { await AvisarAsync("Sem permissão para usar a câmera. Libere nas configurações do celular ou escolha da galeria."); }
        catch (Exception) { await AvisarAsync("Não deu para abrir a câmera. Use \"Escolher da galeria\"."); }
    }

    [RelayCommand]
    private async Task EscolherDaGaleriaAsync()
    {
        try
        {
            var opcoes = Opcoes;
            opcoes.SelectionLimit = 1;
            var fotos = await MediaPicker.Default.PickPhotosAsync(opcoes);
            await UsarFotoAsync(fotos?.FirstOrDefault());
        }
        catch (PermissionException) { await AvisarAsync("Sem permissão para abrir a galeria. Libere nas configurações do celular."); }
        catch (Exception) { await AvisarAsync("Não deu para abrir a galeria."); }
    }

    private async Task UsarFotoAsync(FileResult? foto)
    {
        if (foto is null) return; // a pessoa desistiu
        var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant() == ".png" ? ".png" : ".jpg";
        var destino = Path.Combine(FileSystem.CacheDirectory, $"receita-{Guid.NewGuid():N}{extensao}");
        await using (var origem = await foto.OpenReadAsync())
        await using (var arquivo = File.Create(destino))
            await origem.CopyToAsync(arquivo);

        var tamanho = new FileInfo(destino).Length;
        if (tamanho > TamanhoMaximo)
        {
            File.Delete(destino);
            await AvisarAsync("A foto ficou maior que 5 MB. Tente de novo com menos zoom.");
            return;
        }
        ApagarFoto();
        _arquivoFoto = destino;
        Previa = ImageSource.FromFile(destino);
        TamanhoFoto = $"Foto pronta para enviar ({tamanho / 1024.0:0} KB).";
        TemFoto = true;
    }

    [RelayCommand]
    private void RemoverFoto() => ApagarFoto();

    private void ApagarFoto()
    {
        if (_arquivoFoto is not null && File.Exists(_arquivoFoto)) File.Delete(_arquivoFoto);
        _arquivoFoto = null;
        Previa = null;
        TamanhoFoto = "";
        TemFoto = false;
    }

    private string? PrimeiroErro()
    {
        if (string.IsNullOrWhiteSpace(Nome)) return "Digite seu nome.";
        if (Validacao.SoNumeros(Telefone).Length < 10) return "Digite um celular com DDD.";
        if (Endereco.Trim().Length < 8) return "Digite o endereço completo.";
        if (string.IsNullOrWhiteSpace(Pedido) && !TemFoto) return "Conte o que precisa ou mande a foto da receita.";
        return null;
    }

    [RelayCommand]
    private async Task EnviarAsync()
    {
        if (PrimeiroErro() is { } erro) { await AvisarAsync(erro); return; }

        Ocupado = true;
        try
        {
            using var formulario = new MultipartFormDataContent
            {
                { new StringContent(Nome.Trim()), "nomeCliente" },
                { new StringContent(Telefone.Trim()), "telefone" },
                { new StringContent(Endereco.Trim()), "enderecoTexto" },
                { new StringContent(string.IsNullOrWhiteSpace(Pedido) ? "Ver receita anexada." : Pedido.Trim()), "observacao" }
            };
            if (_arquivoFoto is not null)
            {
                var foto = new StreamContent(File.OpenRead(_arquivoFoto));
                foto.Headers.ContentType = new MediaTypeHeaderValue(_arquivoFoto.EndsWith(".png") ? "image/png" : "image/jpeg");
                formulario.Add(foto, "receita", Path.GetFileName(_arquivoFoto));
            }

            var r = await _api.PostFormularioAsync<DeliveryResposta>("delivery", formulario);
            formulario.Dispose(); // fecha a foto antes de apagar o arquivo temporário
            ApagarFoto();
            Pedido = "";
            await AvisarAsync($"Recebemos sua solicitação nº {r.IdDelivery}. A farmacêutica vai chamar no WhatsApp em poucos minutos com o valor.");
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            await AvisarAsync(ex.SemConexao ? "Sem conexão com a farmácia. A receita não foi enviada; tente de novo com internet." : ex.Message);
        }
        finally { Ocupado = false; }
    }
}
