
using System.Net.Http.Json;
using System.Text.Json;
using DanWeb.Notificacoes.Domain.Entities;
using DanWeb.Notificacoes.Domain.Enums;

namespace DanWeb.Notificacoes.Worker.Services;

public sealed class EvolutionApiService : IEvolutionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ICredencialResolver _credencialResolver;

    public EvolutionApiService(
        HttpClient httpClient,
        ICredencialResolver credencialResolver)
    {
        _httpClient = httpClient;
        _credencialResolver = credencialResolver;
    }

    public async Task<EvolutionApiResultado> EnviarAsync(
        Notificacao notificacao,
        InstanciaEvolution instancia,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!instancia.Ativa)
                throw new InvalidOperationException(
                    "A instância Evolution está inativa.");

            var apiKey = await _credencialResolver.ObterAsync(
                instancia.ReferenciaCredencial,
                cancellationToken);

            var urlBase = instancia.UrlBase.TrimEnd('/');
            var nomeInstancia = Uri.EscapeDataString(
                instancia.NomeInstancia);

            var (endpoint, payload) = CriarRequisicao(notificacao);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{urlBase}{endpoint}/{nomeInstancia}");

            request.Headers.Add("apikey", apiKey);
            request.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var resposta = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;

                return new EvolutionApiResultado
                {
                    Sucesso = false,
                    ErroTemporario =
                        statusCode == 408 ||
                        statusCode == 429 ||
                        statusCode >= 500,
                    RespostaJson = resposta,
                    Erro = $"Evolution API retornou HTTP {statusCode}: {resposta}"
                };
            }

            var idMensagem = ExtrairIdMensagem(resposta);

            return new EvolutionApiResultado
            {
                Sucesso = true,
                IdMensagem = idMensagem,
                RespostaJson = resposta
            };
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return new EvolutionApiResultado
            {
                Sucesso = false,
                ErroTemporario = true,
                Erro = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            return new EvolutionApiResultado
            {
                Sucesso = false,
                ErroTemporario = true,
                Erro = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new EvolutionApiResultado
            {
                Sucesso = false,
                Erro = ex.Message
            };
        }
    }

    private static (string Endpoint, object Payload) CriarRequisicao(
        Notificacao notificacao)
    {
        var numero = notificacao.Destinatario;

        return notificacao.Tipo switch
        {
            TipoNotificacao.WhatsAppTexto => (
                "/message/sendText",
                new
                {
                    number = numero,
                    text = notificacao.Conteudo
                }),

            TipoNotificacao.WhatsAppImagem => (
                "/message/sendMedia",
                new
                {
                    number = numero,
                    mediatype = "image",
                    media = notificacao.UrlMidia,
                    caption = notificacao.Conteudo
                }),

            TipoNotificacao.WhatsAppDocumento => (
                "/message/sendMedia",
                new
                {
                    number = numero,
                    mediatype = "document",
                    media = notificacao.UrlMidia,
                    caption = notificacao.Conteudo
                }),

            _ => throw new NotSupportedException(
                $"Tipo de notificação não suportado: {notificacao.Tipo}")
        };
    }

    private static string? ExtrairIdMensagem(string resposta)
    {
        if (string.IsNullOrWhiteSpace(resposta))
            return null;

        try
        {
            using var json = JsonDocument.Parse(resposta);

            if (json.RootElement.TryGetProperty(
                "key", out var key) &&
                key.TryGetProperty("id", out var id))
            {
                return id.GetString();
            }

            if (json.RootElement.TryGetProperty(
                "message", out var message) &&
                message.TryGetProperty("key", out key) &&
                key.TryGetProperty("id", out id))
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
            // A resposta pode não estar em JSON.
        }

        return null;
    }
}
