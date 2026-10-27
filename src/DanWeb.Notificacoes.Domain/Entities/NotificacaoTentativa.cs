namespace DanWeb.Notificacoes.Domain.Entities;

public class NotificacaoTentativa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NotificacaoId { get; set; }

    public Notificacao Notificacao { get; set; } = null!;

    public Guid? InstanciaEvolutionId { get; set; }

    public int NumeroTentativa { get; set; }

    public DateTime DataInicioUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DataFimUtc { get; set; }

    public bool? Sucesso { get; set; }

    public string? IdMensagemEvolution { get; set; }

    public string? RespostaApi { get; set; }

    public string? Erro { get; set; }
}