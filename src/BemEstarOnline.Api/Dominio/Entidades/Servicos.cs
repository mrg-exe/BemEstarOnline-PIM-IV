namespace BemEstarOnline.Api.Dominio.Entidades;

/// <summary>Entrega programada de um medicamento de uso contínuo.</summary>
public class Assinatura
{
    public int IdAssinatura { get; set; }
    public int IdUsuario { get; set; }
    public int IdMedicamento { get; set; }
    public string NomePaciente { get; set; } = string.Empty;
    public int Quantidade { get; set; } = 1;
    public int PeriodicidadeDias { get; set; } = 30;
    public decimal ValorUnitario { get; set; }
    public decimal DescontoPercentual { get; set; } = RegrasComerciais.DescontoAssinaturaPercentual;
    public string Status { get; set; } = StatusAssinatura.Ativa;
    public DateTime CriadaEm { get; set; } = DateTime.Now;
    public DateTime? ProximaEntrega { get; set; }

    public Usuario? Usuario { get; set; }
    public Medicamento? Medicamento { get; set; }

    public decimal ValorPorEntrega =>
        Math.Round(ValorUnitario * Quantidade * (1 - DescontoPercentual / 100m), 2);

    public void AlterarStatus(string novo)
    {
        if (!StatusAssinatura.Todos.Contains(novo))
            throw new RegraDeNegocioException("Situação de assinatura inválida.");
        if (Status == StatusAssinatura.Cancelada)
            throw new RegraDeNegocioException("Uma assinatura cancelada não pode ser reativada. Crie uma nova.");
        Status = novo;
        if (novo == StatusAssinatura.Ativa && (ProximaEntrega is null || ProximaEntrega < DateTime.Today))
            ProximaEntrega = DateTime.Today.AddDays(1);
    }
}

/// <summary>Pedido livre de entrega rápida, normalmente com foto da receita.</summary>
public class DeliverySolicitacao
{
    public int IdDelivery { get; set; }
    public int? IdUsuario { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string EnderecoTexto { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public string? ReceitaArquivo { get; set; }
    public string Status { get; set; } = StatusDelivery.Recebido;
    public DateTime CriadoEm { get; set; } = DateTime.Now;

    public Usuario? Usuario { get; set; }

    /// <summary>Conta excluída (LGPD): some o que identifica a pessoa e a receita (o arquivo é apagado pelo serviço).</summary>
    public void Anonimizar()
    {
        NomeCliente = "Conta excluída";
        Telefone = "-";
        EnderecoTexto = "Endereço removido";
        Observacao = null;
        ReceitaArquivo = null;
    }
}
