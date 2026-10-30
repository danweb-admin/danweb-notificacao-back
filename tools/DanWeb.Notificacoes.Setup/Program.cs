using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DanWeb.Notificacoes.Domain.Entities;
using DanWeb.Notificacoes.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

var codigo = args.Length > 0 ? args[0].Trim().ToUpperInvariant() : "KERIGMA";
var nome = args.Length > 1 ? args[1].Trim() : "Kerigma Eventos";

if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 50)
    throw new InvalidOperationException("Código inválido.");

if (string.IsNullOrWhiteSpace(nome) || nome.Length > 100)
    throw new InvalidOperationException("Nome inválido.");

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    var caminho = Path.Combine(
        Directory.GetCurrentDirectory(),
        "src",
        "DanWeb.Notificacoes.Api",
        "appsettings.json");

    if (!File.Exists(caminho))
        throw new FileNotFoundException(
            "Não encontrei o appsettings.json da API.", caminho);

    using var json = JsonDocument.Parse(File.ReadAllText(caminho));

    if (json.RootElement.TryGetProperty("ConnectionStrings", out var strings) &&
        strings.TryGetProperty("DefaultConnection", out var connection))
    {
        connectionString = connection.GetString();
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection no appsettings.json " +
        "ou na variável ConnectionStrings__DefaultConnection.");
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new AppDbContext(options);

// Não gerar outra chave se a aplicação já existir.
// A chave original não pode ser recuperada do hash.
var existente = await db.Aplicacoes
    .AsNoTracking()
    .AnyAsync(x => x.Codigo == codigo);

if (existente)
{
    Console.WriteLine(
        $"A aplicação '{codigo}' já existe. Nenhuma alteração foi realizada.");
    return;
}

var apiKey = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(32))
    .TrimEnd('=')
    .Replace('+', '-')
    .Replace('/', '_');

var hash = Convert.ToHexString(
    SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

var aplicacao = new Aplicacao
{
    Id = Guid.NewGuid(),
    Nome = nome,
    Codigo = codigo,
    ApiKeyHash = hash,
    Ativa = true,
    DataCriacao = DateTime.UtcNow
};

db.Aplicacoes.Add(aplicacao);
await db.SaveChangesAsync();

Console.WriteLine();
Console.WriteLine("Aplicação cadastrada com sucesso.");
Console.WriteLine($"ID: {aplicacao.Id}");
Console.WriteLine($"Código: {aplicacao.Codigo}");
Console.WriteLine($"Nome: {aplicacao.Nome}");
Console.WriteLine();
Console.WriteLine("API KEY (copie agora; ela não será exibida novamente):");
Console.WriteLine(apiKey);
Console.WriteLine();
Console.WriteLine("Guarde a chave em um gerenciador de segredos.");