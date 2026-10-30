using DanWeb.Notificacoes.Worker.Configuracoes;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DanWeb.Notificacoes.Worker.Services;

public sealed class RabbitMqConnectionService : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnectionService(
        IOptions<RabbitMqConfiguracao> options)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.User) ||
            string.IsNullOrWhiteSpace(config.Password))
        {
            throw new InvalidOperationException(
                "As credenciais do RabbitMQ não foram configuradas.");
        }

        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            VirtualHost = config.VirtualHost,
            UserName = config.User,
            Password = config.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
    }

    public IConnection Connection => _connection;

    public void Dispose()
    {
        if (_connection.IsOpen)
            _connection.Close();

        _connection.Dispose();
    }
}