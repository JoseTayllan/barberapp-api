using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisponibilidadeBarbeiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisponibilidadesBarbeiros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeiroId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<string>(type: "text", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraFim = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadesBarbeiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisponibilidadesBarbeiros_Barbeiros_BarbeiroId",
                        column: x => x.BarbeiroId,
                        principalTable: "Barbeiros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesBarbeiros_BarbeiroId_DiaSemana",
                table: "DisponibilidadesBarbeiros",
                columns: new[] { "BarbeiroId", "DiaSemana" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisponibilidadesBarbeiros");
        }
    }
}
