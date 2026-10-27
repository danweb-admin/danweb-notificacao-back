namespace DanWeb.Notificacoes.Domain.Enums;

public enum TipoEventoNotificacao
{
    Criada = 1,
    Enfileirada = 2,
    EnvioIniciado = 3,
    AceitaPelaApi = 4,
    Enviada = 5,
    Entregue = 6,
    Lida = 7,
    Falha = 8,
    RetentativaAgendada = 9,
    Cancelada = 10
}