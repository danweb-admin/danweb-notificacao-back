
using System.Text;
using DanWeb.Notificacoes.Infrastructure.Context;
using DanWeb.Notificacoes.Worker.Configuracoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DanWeb.Notificacoes.Worker.Services;

public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionService _rabbitMq;
    private readonly RabbitMqConfiguracao _config;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionService rabbitMq,
        IOptions<RabbitMqConfiguracao> options,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitMq = rabbitMq;
        _config = options.Value;
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

                await PublicarPendentesAsync(stoppingToken);
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
                    "Erro ao publicar mensagens da Outbox.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(
                    Math.Max(1, _config.OutboxIntervalSeconds)),
                stoppingToken);
        }
    }

    private async Task PublicarPendentesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var agora = DateTime.UtcNow;

        var mensagens = await db.OutboxMensagens
            .Where(x =>
                x.DataProcessamentoUtc == null &&
                (x.ProximaTentativaUtc == null ||
                 x.ProximaTentativaUtc <= agora))
            .OrderBy(x => x.DataCriacaoUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (mensagens.Count == 0)
            return;

        using var channel =
            _rabbitMq.Connection.CreateModel();

        channel.ConfirmSelect();

        channel.QueueDeclare(
            queue: _config.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] =
                    "danweb.notificacoes.dlx",

                ["x-dead-letter-routing-key"] =
                    "danweb.notificacoes.whatsapp.dlq"
            });

        foreach (var mensagem in mensagens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var body = Encoding.UTF8.GetBytes(
                    mensagem.PayloadJson);

                var properties = channel.CreateBasicProperties();

                properties.Persistent = true;
                properties.MessageId = mensagem.Id.ToString();
                properties.ContentType = "application/json";
                properties.Type = mensagem.Tipo;

                channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: _config.Queue,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                channel.WaitForConfirmsOrDie(
                    TimeSpan.FromSeconds(5));

                mensagem.DataProcessamentoUtc = DateTime.UtcNow;
                mensagem.UltimoErro = null;
                mensagem.ProximaTentativaUtc = null;

                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Outbox {OutboxId} publicada na fila {Queue}.",
                    mensagem.Id,
                    _config.Queue);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                mensagem.QuantidadeTentativas++;

                mensagem.UltimoErro = ex.Message.Length > 4000
                    ? ex.Message[..4000]
                    : ex.Message;

                var segundos = Math.Min(
                    300,
                    Math.Pow(
                        2,
                        Math.Min(mensagem.QuantidadeTentativas, 8)));

                mensagem.ProximaTentativaUtc =
                    DateTime.UtcNow.AddSeconds(segundos);

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (Exception erroBanco)
                {
                    _logger.LogError(
                        erroBanco,
                        "Não foi possível registrar a falha da Outbox {OutboxId}.",
                        mensagem.Id);
                }

                _logger.LogError(
                    ex,
                    "Falha ao publicar Outbox {OutboxId}. Tentativa {Tentativa}.",
                    mensagem.Id,
                    mensagem.QuantidadeTentativas);

                // O canal pode estar em estado incerto.
                // Interrompe o lote para tentar novamente no próximo ciclo.
                break;
            }
        }
    }
}
