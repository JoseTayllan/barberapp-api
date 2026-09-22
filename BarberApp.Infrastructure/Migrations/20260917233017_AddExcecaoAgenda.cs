using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExcecaoAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExcecoesAgenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeiroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    NaoAtende = table.Column<bool>(type: "boolean", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "interval", nullable: true),
                    HoraFim = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    CridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcecoesAgenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcecoesAgenda_Barbeiros_BarbeiroId",
                        column: x => x.BarbeiroId,
                        principalTable: "Barbeiros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExcecoesAgenda_BarbeiroId_Data",
                table: "ExcecoesAgenda",
                columns: new[] { "BarbeiroId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcecoesAgenda");
        }
    }
}
