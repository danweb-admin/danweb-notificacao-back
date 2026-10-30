using DanWeb.Notificacoes.Domain.Enums;

namespace DanWeb.Notificacoes.Api.Contracts;

public sealed class CriarNotificacaoRequest
{
    public TipoNotificacao Tipo { get; set; }

    public string Destinatario { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public string? UrlMidia { get; set; }

    public string? ReferenciaExterna { get; set; }

    public string ChaveIdempotencia { get; set; } = string.Empty;

    public DateTime? DataAgendamentoUtc { get; set; }
}