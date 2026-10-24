using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecoAgendado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecoAgendado",
                table: "Agendamentos",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agendamentos_PrecoAgendadoPositivo",
                table: "Agendamentos",
                sql: "\"PrecoAgendado\" IS NULL OR \"PrecoAgendado\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Agendamentos_PrecoAgendadoPositivo",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "PrecoAgendado",
                table: "Agendamentos");
        }
    }
}
