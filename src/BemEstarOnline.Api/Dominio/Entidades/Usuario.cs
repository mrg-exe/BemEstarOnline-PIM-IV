namespace BemEstarOnline.Api.Dominio.Entidades;

/// <summary>Pessoa que acessa o sistema: cliente, atendente ou administrador.</summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Nome pelo qual a pessoa trans ou não binária quer ser chamada (Decreto 8.727/2016).
    /// Quando preenchido, é o nome exibido em todas as telas.
    /// </summary>
    public string? NomeSocial { get; set; }

    public DateTime? DataNascimento { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public string Perfil { get; set; } = Perfis.Cliente;
    public bool Ativo { get; set; } = true;

    /// <summary>Senha provisória (dada pelo administrador): a pessoa precisa criar a própria no primeiro acesso.</summary>
    public bool TrocarSenha { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? AtualizadoEm { get; set; }

    public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();
    public Carrinho? Carrinho { get; set; }
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    /// <summary>Nome usado nas telas: o nome social tem prioridade sobre o nome civil.</summary>
    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeSocial) ? Nome : NomeSocial!;

    public bool EhEquipe => Perfis.EhEquipe(Perfil);

    /// <summary>O administrador define (ou redefine) uma senha provisória: troca obrigatória no próximo acesso.</summary>
    public void DefinirSenhaProvisoria(string senhaHash)
    {
        SenhaHash = senhaHash;
        TrocarSenha = true;
        AtualizadoEm = DateTime.Now;
    }

    /// <summary>A própria pessoa escolhe a nova senha (deixa de ser provisória).</summary>
    public void DefinirNovaSenha(string senhaHash)
    {
        SenhaHash = senhaHash;
        TrocarSenha = false;
        AtualizadoEm = DateTime.Now;
    }

    /// <summary>
    /// Exclusão de conta pedida pelo titular (LGPD, art. 18). Os pedidos continuam existindo
    /// (obrigação fiscal), mas sem nenhum dado que identifique a pessoa.
    /// </summary>
    public void Anonimizar(string senhaHashAleatoria)
    {
        Nome = "Conta excluída";
        NomeSocial = null;
        DataNascimento = null;
        Email = $"excluido-{IdUsuario}@anonimo.local";
        Cpf = $"X{IdUsuario:D13}";
        Telefone = null;
        SenhaHash = senhaHashAleatoria;
        Ativo = false;
        AtualizadoEm = DateTime.Now;
    }
}

/// <summary>Endereço de entrega de um usuário.</summary>
public class Endereco
{
    public int IdEndereco { get; set; }
    public int IdUsuario { get; set; }
    public string? Apelido { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool Principal { get; set; }

    /// <summary>Coordenadas do endereço (GPS do navegador/celular ou busca pelo endereço). Usadas no mapa da entrega.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.Now;

    public Usuario? Usuario { get; set; }

    public string TextoCompleto =>
        $"{Logradouro}, {Numero}{(string.IsNullOrWhiteSpace(Complemento) ? "" : " - " + Complemento)} - {Bairro}, {Cidade}/{Estado} - CEP {Cep}";
}
