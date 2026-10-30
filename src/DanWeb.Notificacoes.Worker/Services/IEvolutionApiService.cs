
using DanWeb.Notificacoes.Domain.Entities;

namespace DanWeb.Notificacoes.Worker.Services;

public interface IEvolutionApiService
{
    Task<EvolutionApiResultado> EnviarAsync(
        Notificacao notificacao,
        InstanciaEvolution instancia,
        CancellationToken cancellationToken);
}

public sealed class EvolutionApiResultado
{
    public bool Sucesso { get; set; }

    public bool ErroTemporario { get; set; }

    public string? IdMensagem { get; set; }

    public string? RespostaJson { get; set; }

    public string? Erro { get; set; }
}
