using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DanWeb.Notificacoes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCredencialAplicacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHash",
                table: "Aplicacoes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "Aplicacoes");
        }
    }
}
