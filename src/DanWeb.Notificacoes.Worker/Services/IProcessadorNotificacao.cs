namespace DanWeb.Notificacoes.Worker.Services;

public interface IProcessadorNotificacao
{
    Task ProcessarAsync(
        Guid notificacaoId,
        CancellationToken cancellationToken);
}