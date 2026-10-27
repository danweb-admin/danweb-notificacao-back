using DanWeb.Notificacoes.Domain.Enums;

namespace DanWeb.Notificacoes.Domain.Entities;

public class Notificacao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AplicacaoId { get; set; }

    public Aplicacao Aplicacao { get; set; } = null!;

    public Guid? InstanciaEvolutionId { get; set; }

    public InstanciaEvolution? InstanciaEvolution { get; set; }

    public TipoNotificacao Tipo { get; set; }

    public StatusNotificacao Status { get; set; }
        = StatusNotificacao.Pendente;

    public string Destinatario { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public string? UrlMidia { get; set; }

    public string? ReferenciaExterna { get; set; }

    public string ChaveIdempotencia { get; set; } = string.Empty;

    public string? IdMensagemEvolution { get; set; }

    public int QuantidadeTentativas { get; set; }

    public DateTime DataCriacaoUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DataAgendamentoUtc { get; set; }

    public DateTime? DataEnvioUtc { get; set; }

    public DateTime? DataEntregaUtc { get; set; }

    public DateTime? DataLeituraUtc { get; set; }

    public DateTime? DataAtualizacaoUtc { get; set; }

    public string? UltimoErro { get; set; }

    public ICollection<NotificacaoTentativa> Tentativas { get; set; }
        = new List<NotificacaoTentativa>();

    public ICollection<NotificacaoEvento> Eventos { get; set; }
        = new List<NotificacaoEvento>();
}