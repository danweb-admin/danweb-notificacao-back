
namespace DanWeb.Notificacoes.Worker.Services;

public sealed class DockerSecretResolver : ICredencialResolver
{
    public async Task<string> ObterAsync(
        string referencia,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(referencia))
            throw new InvalidOperationException(
                "Referência da credencial não informada.");

        // Primeiro tenta obter por variável de ambiente.
        var nomeVariavel = $"EVOLUTION_SECRET_{referencia}";
        var valor = Environment.GetEnvironmentVariable(nomeVariavel);

        if (!string.IsNullOrWhiteSpace(valor))
            return valor.Trim();

        // Depois tenta obter por Docker Secret.
        if (referencia.Contains('/') ||
            referencia.Contains('\\') ||
            referencia.Contains(".."))
        {
            throw new InvalidOperationException(
                "Referência de credencial inválida.");
        }

        var caminho = Path.Combine("/run/secrets", referencia);

        if (File.Exists(caminho))
        {
            valor = await File.ReadAllTextAsync(
                caminho, cancellationToken);

            if (!string.IsNullOrWhiteSpace(valor))
                return valor.Trim();
        }

        throw new InvalidOperationException(
            $"Credencial '{referencia}' não encontrada.");
    }
}
