using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonEventos.Migrations
{
    /// <inheritdoc />
    public partial class EventoHora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Hora",
                table: "Eventos",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hora",
                table: "Eventos");
        }
    }
}
