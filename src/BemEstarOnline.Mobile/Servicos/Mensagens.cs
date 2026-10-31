namespace BemEstarOnline.Mobile.Servicos;

/// <summary>
/// Aviso entre telas (WeakReferenceMessenger): a mesma tela pode estar aberta em duas abas
/// (ex.: Assinaturas pelo Perfil e pelo produto). Quem muda os dados avisa; as outras recarregam.
/// </summary>
public record EnderecosMudaram(object Origem);

public record AssinaturasMudaram(object Origem);
