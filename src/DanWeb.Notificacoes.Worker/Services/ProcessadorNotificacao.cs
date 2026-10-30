
using System.Text.Json;
using DanWeb.Notificacoes.Domain.Entities;
using DanWeb.Notificacoes.Domain.Enums;
using DanWeb.Notificacoes.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace DanWeb.Notificacoes.Worker.Services;

public sealed class ProcessadorNotificacao : IProcessadorNotificacao
{
    private const int LimiteTentativas = 5;
    private const string TipoOutbox = "NotificacaoCriada";

    private readonly AppDbContext _db;
    private readonly IEvolutionApiService _evolutionApi;

    public ProcessadorNotificacao(
        AppDbContext db,
        IEvolutionApiService evolutionApi)
    {
        _db = db;
        _evolutionApi = evolutionApi;
    }

    public async Task ProcessarAsync(
        Guid notificacaoId,
        CancellationToken cancellationToken)
    {
        var notificacao = await _db.Notificacoes
            .FirstOrDefaultAsync(
                x => x.Id == notificacaoId,
                cancellationToken);

        if (notificacao == null)
            return;

        // Uma notificação aceita pela Evolution não deve ser enviada novamente.
        if (notificacao.Status == StatusNotificacao.AceitaPelaApi ||
            notificacao.Status == StatusNotificacao.Enviada ||
            notificacao.Status == StatusNotificacao.Entregue ||
            notificacao.Status == StatusNotificacao.Lida ||
            notificacao.Status == StatusNotificacao.Falha ||
            notificacao.Status == StatusNotificacao.Cancelada)
        {
            return;
        }

        if (notificacao.Status != StatusNotificacao.Pendente &&
            notificacao.Status != StatusNotificacao.AguardandoRetentativa)
        {
            return;
        }

        if (!notificacao.InstanciaEvolutionId.HasValue)
        {
            await RegistrarFalhaDefinitivaAsync(
                notificacao,
                "Notificação sem instância Evolution vinculada.",
                cancellationToken);

            return;
        }

        var instancia = await _db.InstanciasEvolution
            .FirstOrDefaultAsync(
                x => x.Id == notificacao.InstanciaEvolutionId.Value,
                cancellationToken);

        if (instancia == null || !instancia.Ativa)
        {
            await RegistrarFalhaDefinitivaAsync(
                notificacao,
                "Instância Evolution não encontrada ou inativa.",
                cancellationToken);

            return;
        }

        var numeroTentativa = notificacao.QuantidadeTentativas + 1;

        var tentativa = new NotificacaoTentativa
        {
            NotificacaoId = notificacao.Id,
            InstanciaEvolutionId = instancia.Id,
            NumeroTentativa = numeroTentativa,
            DataInicioUtc = DateTime.UtcNow
        };

        notificacao.Status = StatusNotificacao.Processando;
        notificacao.QuantidadeTentativas = numeroTentativa;
        notificacao.DataAtualizacaoUtc = DateTime.UtcNow;

        _db.NotificacaoTentativas.Add(tentativa);

        AdicionarEvento(
            notificacao.Id,
            TipoEventoNotificacao.EnvioIniciado,
            $"Iniciando tentativa {numeroTentativa}.");

        await _db.SaveChangesAsync(cancellationToken);

        EvolutionApiResultado resultado;

        try
        {
            resultado = await _evolutionApi.EnviarAsync(
                notificacao,
                instancia,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            resultado = new EvolutionApiResultado
            {
                Sucesso = false,
                ErroTemporario = true,
                Erro = ex.Message
            };
        }

        tentativa.DataFimUtc = DateTime.UtcNow;
        tentativa.RespostaApi = resultado.RespostaJson;
        tentativa.IdMensagemEvolution = resultado.IdMensagem;
        tentativa.Sucesso = resultado.Sucesso;
        tentativa.Erro = resultado.Erro;

        if (resultado.Sucesso)
        {
            notificacao.Status = StatusNotificacao.AceitaPelaApi;
            notificacao.IdMensagemEvolution = resultado.IdMensagem;
            notificacao.DataAtualizacaoUtc = DateTime.UtcNow;
            notificacao.UltimoErro = null;

            AdicionarEvento(
                notificacao.Id,
                TipoEventoNotificacao.AceitaPelaApi,
                "Evolution API aceitou a solicitação de envio.",
                resultado.RespostaJson);

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        await TratarFalhaAsync(
            notificacao,
            tentativa,
            resultado,
            cancellationToken);
    }

    private async Task TratarFalhaAsync(
        Notificacao notificacao,
        NotificacaoTentativa tentativa,
        EvolutionApiResultado resultado,
        CancellationToken cancellationToken)
    {
        var erro = resultado.Erro ?? "Erro desconhecido no envio.";

        notificacao.UltimoErro = erro.Length > 2000
            ? erro[..2000]
            : erro;

        notificacao.DataAtualizacaoUtc = DateTime.UtcNow;

        var podeRetentar =
            resultado.ErroTemporario &&
            notificacao.QuantidadeTentativas < LimiteTentativas;

        if (podeRetentar)
        {
            var segundos = Math.Min(
                30 * Math.Pow(2, notificacao.QuantidadeTentativas - 1),
                300);

            var proximaTentativa = DateTime.UtcNow.AddSeconds(segundos);

            notificacao.Status = StatusNotificacao.AguardandoRetentativa;
            notificacao.DataAgendamentoUtc = proximaTentativa;

            var payload = JsonSerializer.Serialize(new
            {
                notificacaoId = notificacao.Id
            });

            _db.OutboxMensagens.Add(new OutboxMensagem
            {
                Id = Guid.NewGuid(),
                Tipo = TipoOutbox,
                PayloadJson = payload,
                DataCriacaoUtc = DateTime.UtcNow,
                ProximaTentativaUtc = proximaTentativa
            });

            AdicionarEvento(
                notificacao.Id,
                TipoEventoNotificacao.RetentativaAgendada,
                $"Retentativa {notificacao.QuantidadeTentativas + 1} agendada para {proximaTentativa:O}. Erro: {erro}");
        }
        else
        {
            notificacao.Status = StatusNotificacao.Falha;

            AdicionarEvento(
                notificacao.Id,
                TipoEventoNotificacao.Falha,
                $"Falha definitiva no envio. Erro: {erro}");
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RegistrarFalhaDefinitivaAsync(
        Notificacao notificacao,
        string erro,
        CancellationToken cancellationToken)
    {
        notificacao.Status = StatusNotificacao.Falha;
        notificacao.UltimoErro = erro;
        notificacao.DataAtualizacaoUtc = DateTime.UtcNow;

        AdicionarEvento(
            notificacao.Id,
            TipoEventoNotificacao.Falha,
            erro);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private void AdicionarEvento(
        Guid notificacaoId,
        TipoEventoNotificacao tipo,
        string descricao,
        string? payloadJson = null)
    {
        _db.NotificacaoEventos.Add(new NotificacaoEvento
        {
            NotificacaoId = notificacaoId,
            Tipo = tipo,
            Descricao = descricao,
            PayloadJson = payloadJson,
            DataEventoUtc = DateTime.UtcNow
        });
    }
}
