using DanWeb.Notificacoes.Domain.Enums;

namespace DanWeb.Notificacoes.Domain.Entities;

public class NotificacaoEvento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NotificacaoId { get; set; }

    public Notificacao Notificacao { get; set; } = null!;

    public TipoEventoNotificacao Tipo { get; set; }

    public string? Descricao { get; set; }

    public string? PayloadJson { get; set; }

    public DateTime DataEventoUtc { get; set; } = DateTime.UtcNow;
}