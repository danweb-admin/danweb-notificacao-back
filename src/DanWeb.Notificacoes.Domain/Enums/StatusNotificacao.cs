namespace DanWeb.Notificacoes.Domain.Enums;

public enum StatusNotificacao
{
    Pendente = 1,
    Processando = 2,
    AceitaPelaApi = 3,
    Enviada = 4,
    Entregue = 5,
    Lida = 6,
    AguardandoRetentativa = 7,
    Falha = 8,
    Cancelada = 9
}