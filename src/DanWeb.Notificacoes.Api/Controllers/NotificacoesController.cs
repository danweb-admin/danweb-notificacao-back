using System.Text.Json;
using DanWeb.Notificacoes.Api.Contracts;
using DanWeb.Notificacoes.Api.Services;
using DanWeb.Notificacoes.Domain.Entities;
using DanWeb.Notificacoes.Domain.Enums;
using DanWeb.Notificacoes.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DanWeb.Notificacoes.Api.Controllers;

[ApiController]
[Route("api/notificacoes")]
public sealed class NotificacoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiKeyService _apiKeyService;

    public NotificacoesController(
        AppDbContext db,
        ApiKeyService apiKeyService)
    {
        _db = db;
        _apiKeyService = apiKeyService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarNotificacaoRequest request,
        CancellationToken cancellationToken)
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { mensagem = "Credencial não informada." });

        var aplicacaoId = await _apiKeyService.ObterAplicacaoIdAsync(
            apiKey, cancellationToken);

        if (aplicacaoId is null)
            return Unauthorized(new { mensagem = "Credencial inválida." });

        if (string.IsNullOrWhiteSpace(request.Destinatario) ||
            request.Destinatario.Length > 30)
        {
            return BadRequest(new
            {
                mensagem = "Destinatário inválido."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ChaveIdempotencia) ||
            request.ChaveIdempotencia.Length > 100)
        {
            return BadRequest(new
            {
                mensagem = "Informe uma chave de idempotência de até 100 caracteres."
            });
        }

        if (!Enum.IsDefined(typeof(TipoNotificacao), request.Tipo))
        {
            return BadRequest(new
            {
                mensagem = "Tipo de notificação inválido."
            });
        }

        if (request.Tipo == TipoNotificacao.WhatsAppTexto &&
            string.IsNullOrWhiteSpace(request.Conteudo))
        {
            return BadRequest(new
            {
                mensagem = "O conteúdo é obrigatório para mensagens de texto."
            });
        }

        if (request.Tipo != TipoNotificacao.WhatsAppTexto &&
            string.IsNullOrWhiteSpace(request.UrlMidia))
        {
            return BadRequest(new
            {
                mensagem = "Informe a URL da mídia."
            });
        }

        if (request.Conteudo.Length > 10000)
        {
            return BadRequest(new
            {
                mensagem = "O conteúdo excede o limite permitido."
            });
        }

        var existente = await _db.Notificacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.AplicacaoId == aplicacaoId.Value &&
                     x.ChaveIdempotencia == request.ChaveIdempotencia,
                cancellationToken);

        if (existente is not null)
        {
            return Ok(new
            {
                id = existente.Id,
                status = existente.Status.ToString(),
                mensagem = "Esta solicitação já foi registrada."
            });
        }

        var notificacao = new Notificacao
        {
            AplicacaoId = aplicacaoId.Value,
            Tipo = request.Tipo,
            Destinatario = request.Destinatario.Trim(),
            Conteudo = request.Conteudo,
            UrlMidia = request.UrlMidia,
            ReferenciaExterna = request.ReferenciaExterna,
            ChaveIdempotencia = request.ChaveIdempotencia,
            DataAgendamentoUtc = request.DataAgendamentoUtc,
            Status = request.DataAgendamentoUtc.HasValue &&
                     request.DataAgendamentoUtc.Value > DateTime.UtcNow
                ? StatusNotificacao.Pendente
                : StatusNotificacao.Pendente
        };

        var outbox = new OutboxMensagem
        {
            Tipo = "NotificacaoCriada",
            PayloadJson = JsonSerializer.Serialize(new
            {
                notificacaoId = notificacao.Id
            })
        };

        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.Notificacoes.Add(notificacao);
        _db.OutboxMensagens.Add(outbox);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Accepted(new
        {
            id = notificacao.Id,
            status = notificacao.Status.ToString(),
            mensagem = "Notificação registrada para processamento."
        });
    }
}