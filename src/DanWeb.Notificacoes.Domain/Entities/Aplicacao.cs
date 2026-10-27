namespace DanWeb.Notificacoes.Domain.Entities;

public class Aplicacao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nome { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<Notificacao> Notificacoes { get; set; }
        = new List<Notificacao>();
}