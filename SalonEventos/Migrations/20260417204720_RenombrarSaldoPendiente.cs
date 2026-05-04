using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonEventos.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarSaldoPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SaldoPendiente",
                table: "Eventos",
                newName: "MontoRestante");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Movimientos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MontoRestante",
                table: "Eventos",
                newName: "SaldoPendiente");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Movimientos",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

