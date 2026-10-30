using DanWeb.Notificacoes.Infrastructure.Context;
using DanWeb.Notificacoes.Worker.Configuracoes;
using DanWeb.Notificacoes.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqConfiguracao>(
    builder.Configuration.GetSection("RabbitMQ"));

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton<RabbitMqConnectionService>();

builder.Services.AddSingleton<
    ICredencialResolver,
    DockerSecretResolver>();

builder.Services.AddHttpClient<
    IEvolutionApiService,
    EvolutionApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<
    IProcessadorNotificacao,
    ProcessadorNotificacao>();

builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

var host = builder.Build();

host.Run();