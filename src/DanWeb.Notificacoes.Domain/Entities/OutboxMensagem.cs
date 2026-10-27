namespace DanWeb.Notificacoes.Domain.Entities;

public class OutboxMensagem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Tipo { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime DataCriacaoUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DataProcessamentoUtc { get; set; }

    public int QuantidadeTentativas { get; set; }

    public DateTime? ProximaTentativaUtc { get; set; }

    public string? UltimoErro { get; set; }
}