using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DanWeb.Notificacoes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aplicacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aplicacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstanciasEvolution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UrlBase = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NomeInstancia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReferenciaCredencial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    Prioridade = table.Column<int>(type: "int", nullable: false),
                    IntervaloMinimoSegundos = table.Column<int>(type: "int", nullable: false),
                    UltimaVerificacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataCriacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanciasEvolution", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMensagens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCriacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataProcessamentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuantidadeTentativas = table.Column<int>(type: "int", nullable: false),
                    ProximaTentativaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoErro = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMensagens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AplicacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanciaEvolutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UrlMidia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenciaExterna = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ChaveIdempotencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdMensagemEvolution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QuantidadeTentativas = table.Column<int>(type: "int", nullable: false),
                    DataCriacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAgendamentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataEnvioUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataEntregaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataLeituraUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataAtualizacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoErro = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Aplicacoes_AplicacaoId",
                        column: x => x.AplicacaoId,
                        principalTable: "Aplicacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notificacoes_InstanciasEvolution_InstanciaEvolutionId",
                        column: x => x.InstanciaEvolutionId,
                        principalTable: "InstanciasEvolution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificacaoEventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataEventoUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacaoEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacaoEventos_Notificacoes_NotificacaoId",
                        column: x => x.NotificacaoId,
                        principalTable: "Notificacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificacaoTentativas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanciaEvolutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumeroTentativa = table.Column<int>(type: "int", nullable: false),
                    DataInicioUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFimUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sucesso = table.Column<bool>(type: "bit", nullable: true),
                    IdMensagemEvolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespostaApi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Erro = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacaoTentativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacaoTentativas_Notificacoes_NotificacaoId",
                        column: x => x.NotificacaoId,
                        principalTable: "Notificacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aplicacoes_Codigo",
                table: "Aplicacoes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstanciasEvolution_UrlBase_NomeInstancia",
                table: "InstanciasEvolution",
                columns: new[] { "UrlBase", "NomeInstancia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoEventos_NotificacaoId_DataEventoUtc",
                table: "NotificacaoEventos",
                columns: new[] { "NotificacaoId", "DataEventoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoTentativas_NotificacaoId_NumeroTentativa",
                table: "NotificacaoTentativas",
                columns: new[] { "NotificacaoId", "NumeroTentativa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_AplicacaoId_ChaveIdempotencia",
                table: "Notificacoes",
                columns: new[] { "AplicacaoId", "ChaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_InstanciaEvolutionId",
                table: "Notificacoes",
                column: "InstanciaEvolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_Status_DataAgendamentoUtc",
                table: "Notificacoes",
                columns: new[] { "Status", "DataAgendamentoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMensagens_DataProcessamentoUtc_ProximaTentativaUtc",
                table: "OutboxMensagens",
                columns: new[] { "DataProcessamentoUtc", "ProximaTentativaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacaoEventos");

            migrationBuilder.DropTable(
                name: "NotificacaoTentativas");

            migrationBuilder.DropTable(
                name: "OutboxMensagens");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "Aplicacoes");

            migrationBuilder.DropTable(
                name: "InstanciasEvolution");
        }
    }
}
