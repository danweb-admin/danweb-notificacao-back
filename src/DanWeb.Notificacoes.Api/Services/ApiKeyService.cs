using System.Security.Cryptography;
using System.Text;
using DanWeb.Notificacoes.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace DanWeb.Notificacoes.Api.Services;

public sealed class ApiKeyService
{
    private readonly AppDbContext _dbContext;

    public ApiKeyService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> ObterAplicacaoIdAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

        // As credenciais são comparadas por hash.
        // A consulta usa a igualdade do banco para localizar a aplicação.
        var aplicacoes = await _dbContext.Aplicacoes
            .AsNoTracking()
            .Where(x => x.Ativa)
            .Select(x => new { x.Id, x.ApiKeyHash })
            .ToListAsync(cancellationToken);

        var hashBytes = Encoding.ASCII.GetBytes(hash);

        foreach (var aplicacao in aplicacoes)
        {
            var candidato = Encoding.ASCII.GetBytes(
                aplicacao.ApiKeyHash ?? string.Empty);

            if (candidato.Length == hashBytes.Length &&
                CryptographicOperations.FixedTimeEquals(
                    candidato, hashBytes))
            {
                return aplicacao.Id;
            }
        }

        return null;
    }
}