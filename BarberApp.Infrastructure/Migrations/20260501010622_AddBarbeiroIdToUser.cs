using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBarbeiroIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BarbeiroId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarbeiroId",
                table: "AspNetUsers");
        }
    }
}
