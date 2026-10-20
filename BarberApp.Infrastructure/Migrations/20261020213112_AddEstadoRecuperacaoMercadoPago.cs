using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoRecuperacaoMercadoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequerReautorizacao",
                table: "ConexoesMercadoPago",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequerReautorizacao",
                table: "ConexoesMercadoPago");
        }
    }
}
