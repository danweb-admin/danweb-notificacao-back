
using System.Text;
using System.Text.Json;
using DanWeb.Notificacoes.Worker.Configuracoes;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DanWeb.Notificacoes.Worker.Services;

public sealed class RabbitMqConsumerService : BackgroundService
{
    private const string DeadLetterExchange =
        "danweb.notificacoes.dlx";

    private const string DeadLetterQueue =
        "danweb.notificacoes.whatsapp.dlq";

    private readonly RabbitMqConnectionService _rabbitMq;
    private readonly RabbitMqConfiguracao _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerService> _logger;

    public RabbitMqConsumerService(
        RabbitMqConnectionService rabbitMq,
        IOptions<RabbitMqConfiguracao> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerService> logger)
    {
        _rabbitMq = rabbitMq;
        _config = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_rabbitMq.Connection.IsOpen)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);

                    continue;
                }

                using var channel =
                    _rabbitMq.Connection.CreateModel();

                ConfigurarFilas(channel);

                channel.BasicQos(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false);

                var consumer =
                    new AsyncEventingBasicConsumer(channel);

                consumer.Received += async (_, eventArgs) =>
                {
                    var body = Encoding.UTF8.GetString(
                        eventArgs.Body.ToArray());

                    try
                    {
                        var mensagem =
                            JsonSerializer.Deserialize<MensagemFila>(
                                body);

                        if (mensagem == null ||
                            mensagem.NotificacaoId == Guid.Empty)
                        {
                            throw new InvalidOperationException(
                                "Mensagem RabbitMQ inválida.");
                        }

                        using var scope =
                            _scopeFactory.CreateScope();

                        var processador = scope.ServiceProvider
                            .GetRequiredService<IProcessadorNotificacao>();

                        await processador.ProcessarAsync(
                            mensagem.NotificacaoId,
                            stoppingToken);

                        channel.BasicAck(
                            eventArgs.DeliveryTag,
                            multiple: false);

                        _logger.LogInformation(
                            "Notificação {NotificacaoId} processada.",
                            mensagem.NotificacaoId);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        if (channel.IsOpen)
                        {
                            channel.BasicNack(
                                eventArgs.DeliveryTag,
                                multiple: false,
                                requeue: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Erro ao processar mensagem RabbitMQ. A mensagem será encaminhada para DLQ.");

                        if (channel.IsOpen)
                        {
                            channel.BasicNack(
                                eventArgs.DeliveryTag,
                                multiple: false,
                                requeue: false);
                        }
                    }
                };

                channel.BasicConsume(
                    queue: _config.Queue,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation(
                    "Consumidor conectado à fila {Fila}.",
                    _config.Queue);

                while (!stoppingToken.IsCancellationRequested &&
                       channel.IsOpen)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(1),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha no consumidor RabbitMQ. Nova tentativa em 5 segundos.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }
    }

    private void ConfigurarFilas(IModel channel)
    {
        channel.ExchangeDeclare(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: DeadLetterQueue);

        var argumentos = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = DeadLetterQueue
        };

        channel.QueueDeclare(
            queue: _config.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: argumentos);
    }

    private sealed class MensagemFila
    {
        public Guid NotificacaoId { get; set; }
    }
}
