using System.ComponentModel.DataAnnotations;

namespace SalonEventos.Models
{
    public class Evento
    {
        public int Id { get; set; }

        public string NombreCliente { get; set; }

        public string Telefono { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; }

        public string TipoEvento { get; set; }

        public int CantidadInvitados { get; set; }

        public decimal MontoTotal { get; set; }

        public decimal Senia { get; set; }

        public decimal SaldoPendiente { get; set; }

        public string Estado { get; set; } = "Reservado";

        // 🔥 RELACION
        public List<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
    }
}