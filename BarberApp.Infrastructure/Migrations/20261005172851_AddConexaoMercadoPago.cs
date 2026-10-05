using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConexaoMercadoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConexoesMercadoPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    AdministradorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TokensProtegidos = table.Column<string>(type: "text", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sandbox = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConexoesMercadoPago", x => x.Id);
                    table.CheckConstraint("CK_ConexoesMercadoPago_UnicaInstalacao", "\"Id\" = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConexoesMercadoPago");
        }
    }
}
