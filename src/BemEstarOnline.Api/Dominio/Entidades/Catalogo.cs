namespace BemEstarOnline.Api.Dominio.Entidades;

/// <summary>Agrupamento do catálogo (ex.: Genéricos, Uso contínuo, Higiene).</summary>
public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Icone { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<Medicamento> Medicamentos { get; set; } = new List<Medicamento>();
}

/// <summary>Produto vendido pela farmácia (medicamento, cosmético, correlato ou aparelho).</summary>
public class Medicamento
{
    public int IdMedicamento { get; set; }
    public int IdCategoria { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? PrincipioAtivo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Dosagem { get; set; }
    public string? Apresentacao { get; set; }
    public string Fabricante { get; set; } = string.Empty;
    public string? ViaAdministracao { get; set; }
    public string? RegistroAnvisa { get; set; }
    public DateTime? ValidadeLote { get; set; }
    public decimal Preco { get; set; }
    public string? ImagemUrl { get; set; }
    public bool ExigeReceita { get; set; }
    public bool UsoContinuo { get; set; }

    /// <summary>Produto disponível pelo programa federal Farmácia Popular.</summary>
    public bool FarmaciaPopular { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? AtualizadoEm { get; set; }

    public Categoria? Categoria { get; set; }
    public Estoque? Estoque { get; set; }

    public int QuantidadeEmEstoque => Estoque?.Quantidade ?? 0;
    public bool Disponivel => Ativo && QuantidadeEmEstoque > 0;
}

/// <summary>Quantidade em estoque de cada produto e o ponto mínimo para reposição.</summary>
public class Estoque
{
    public int IdEstoque { get; set; }
    public int IdMedicamento { get; set; }
    public int Quantidade { get; set; }
    public int QuantidadeMinima { get; set; } = 5;
    public string? Localizacao { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;

    public Medicamento? Medicamento { get; set; }

    public bool AbaixoDoMinimo => Quantidade <= QuantidadeMinima;
}

/// <summary>Aviso de reposição gerado automaticamente quando o estoque chega ao mínimo.</summary>
public class AlertaEstoque
{
    public int IdAlerta { get; set; }
    public int IdMedicamento { get; set; }
    public int QuantidadeAtual { get; set; }
    public int QuantidadeMinima { get; set; }
    public bool Resolvido { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? ResolvidoEm { get; set; }

    public Medicamento? Medicamento { get; set; }
}
