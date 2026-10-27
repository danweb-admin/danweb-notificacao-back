namespace DanWeb.Notificacoes.Domain.Entities;

public class InstanciaEvolution
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nome { get; set; } = string.Empty;

    public string UrlBase { get; set; } = string.Empty;

    public string NomeInstancia { get; set; } = string.Empty;

    // Referência a um segredo protegido.
    // Não armazenar a chave da Evolution em texto puro.
    public string ReferenciaCredencial { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;

    public int Prioridade { get; set; } = 1;

    public int IntervaloMinimoSegundos { get; set; } = 5;

    public DateTime? UltimaVerificacaoUtc { get; set; }

    public DateTime DataCriacaoUtc { get; set; } = DateTime.UtcNow;
}
