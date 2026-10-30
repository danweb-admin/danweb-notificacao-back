namespace DanWeb.Notificacoes.Worker.Configuracoes;

public sealed class RabbitMqConfiguracao
{
    public string Host { get; set; } = "rabbitmq";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Queue { get; set; } =
        "danweb.notificacoes.whatsapp";

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public ushort PrefetchCount { get; set; } = 1;

    public int OutboxIntervalSeconds { get; set; } = 5;
}