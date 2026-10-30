
namespace DanWeb.Notificacoes.Worker.Services;

public interface ICredencialResolver
{
    Task<string> ObterAsync(
        string referencia,
        CancellationToken cancellationToken);
}
