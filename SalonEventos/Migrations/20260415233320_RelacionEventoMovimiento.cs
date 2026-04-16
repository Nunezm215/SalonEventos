using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonEventos.Migrations
{
    /// <inheritdoc />
    public partial class RelacionEventoMovimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Movimientos",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventoId",
                table: "Movimientos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_EventoId",
                table: "Movimientos",
                column: "EventoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_Eventos_EventoId",
                table: "Movimientos",
                column: "EventoId",
                principalTable: "Eventos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_Eventos_EventoId",
                table: "Movimientos");

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_EventoId",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "EventoId",
                table: "Movimientos");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Movimientos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
