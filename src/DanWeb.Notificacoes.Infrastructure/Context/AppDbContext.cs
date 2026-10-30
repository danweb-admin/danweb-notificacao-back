using DanWeb.Notificacoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DanWeb.Notificacoes.Infrastructure.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Aplicacao> Aplicacoes => Set<Aplicacao>();

    public DbSet<InstanciaEvolution> InstanciasEvolution =>
        Set<InstanciaEvolution>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    public DbSet<NotificacaoTentativa> NotificacaoTentativas =>
        Set<NotificacaoTentativa>();

    public DbSet<NotificacaoEvento> NotificacaoEventos =>
        Set<NotificacaoEvento>();

    public DbSet<OutboxMensagem> OutboxMensagens =>
        Set<OutboxMensagem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Aplicacao>(entity =>
        {
            entity.ToTable("Aplicacoes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique();

            entity.Property(x => x.ApiKeyHash)
                .HasMaxLength(64)
                .IsRequired();
        });

        modelBuilder.Entity<InstanciaEvolution>(entity =>
        {
            entity.ToTable("InstanciasEvolution");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.UrlBase)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.NomeInstancia)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.ReferenciaCredencial)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.UrlBase,
                x.NomeInstancia
            }).IsUnique();
        });

        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.ToTable("Notificacoes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Destinatario)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Conteudo)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(x => x.ReferenciaExterna)
                .HasMaxLength(150);

            entity.Property(x => x.ChaveIdempotencia)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.IdMensagemEvolution)
                .HasMaxLength(200);

            entity.Property(x => x.UltimoErro)
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.AplicacaoId,
                x.ChaveIdempotencia
            }).IsUnique();

            entity.HasIndex(x => new
            {
                x.Status,
                x.DataAgendamentoUtc
            });

            entity.HasOne(x => x.Aplicacao)
                .WithMany(x => x.Notificacoes)
                .HasForeignKey(x => x.AplicacaoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.InstanciaEvolution)
                .WithMany()
                .HasForeignKey(x => x.InstanciaEvolutionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificacaoTentativa>(entity =>
        {
            entity.ToTable("NotificacaoTentativas");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RespostaApi)
                .HasColumnType("nvarchar(max)");

            entity.Property(x => x.Erro)
                .HasMaxLength(4000);

            entity.HasIndex(x => new
            {
                x.NotificacaoId,
                x.NumeroTentativa
            }).IsUnique();

            entity.HasOne(x => x.Notificacao)
                .WithMany(x => x.Tentativas)
                .HasForeignKey(x => x.NotificacaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificacaoEvento>(entity =>
        {
            entity.ToTable("NotificacaoEventos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Descricao)
                .HasMaxLength(1000);

            entity.Property(x => x.PayloadJson)
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(x => new
            {
                x.NotificacaoId,
                x.DataEventoUtc
            });

            entity.HasOne(x => x.Notificacao)
                .WithMany(x => x.Eventos)
                .HasForeignKey(x => x.NotificacaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutboxMensagem>(entity =>
        {
            entity.ToTable("OutboxMensagens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Tipo)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PayloadJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(x => x.UltimoErro)
                .HasMaxLength(4000);

            entity.HasIndex(x => new
            {
                x.DataProcessamentoUtc,
                x.ProximaTentativaUtc
            });
        });
    }
}